using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ChatAppCommon;

namespace ServerApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// TCPサーバー。
        /// </summary>
        private TcpListenerEx Server { get; set; }

        /// <summary>
        /// 接続中のクライアントリスト
        /// </summary>
        private SynchronizedCollection<TcpClientEx> ClientList { get; set; }


        public MainWindow()
        {
            InitializeComponent();

            ClientList= new SynchronizedCollection<TcpClientEx>();
        }
        
        private void MaineWindow_Load(object sender, EventArgs e)
        {
            // 接続状態設定
            SetConnectionStatus(false);

            txtConnectingClient.Text = "No Connecting";
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            var port = txtPort.Text.Trim();

            try
            {
                // 接続情報有効チェック
                if (!CheckConnectionSettings(port)) return;

                // サーバーを作成して監視開始
                var localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), int.Parse(port));
                Server = new TcpListenerEx(localEndPoint);
                Server.Start();

                // 接続受付ループ開始
                _ = AsyncAcceptWaitLoop();

                // 接続状態設定
                SetConnectionStatus(true);

                // ボタンの有効状態を設定
                btnStart.IsEnabled= false;
                txtPort.IsEnabled= false;
                btnEnd.IsEnabled= true;

            }
            catch(SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                MessageBox.Show("指定したポートは他のシステムに使用されています。");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btnEnd_Click(object sender, RoutedEventArgs e)
        {
            // サーバー停止
            Server?.Stop();
            Server = null;

            // 接続状態設定
            SetConnectionStatus(false);

            // 接続中のクライアントリストをクリア
            txtConnectingClient.Text = "No Connecting";


            // 接続中クライアントを全て閉じる
            lock (ClientList.SyncRoot)
            {
                foreach(var client in ClientList)
                {
                    client.Socket.Close();
                }
                ClientList.Clear();
            }

            // ボタンの有効状態を設定
            btnStart.IsEnabled= true;
            txtPort.IsEnabled= true;
            btnEnd.IsEnabled= false;
        }

        private void SetConnectionStatus(bool connection)
        {
            // 状態設定
            Dispatcher.Invoke(new Action(() => lblStatus.Content = $"状態：{(connection ? "監視中" : "停止")}"));
        } 

        private void SetConnectingClient()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                txtConnectingClient.Text = "";
                foreach (var client in ClientList)
                {
                    // 接続中クライアント追加
                    txtConnectingClient.Text = txtConnectingClient.Text + client.RemoteEndPoint + "\r\n";
                }

                if(txtConnectingClient.Text == "")
                {
                    txtConnectingClient.Text = "No Connecting";
                }
            }));
        }

        private bool CheckConnectionSettings(string port)
        {
            // ポート番号空チェック
            if (string.IsNullOrEmpty(port))
            {
                MessageBox.Show("ポート番号が空です。");
                return false;
            }

            // ポート番号数値チェック
            if(!Regex.IsMatch(port, "^[0-9]+$"))
            {
                MessageBox.Show("ポート番号は数値を指定してください。");
                return false;
            }

            // ポート番号有効値チェック
            var portNum = int.Parse(port);
            if(portNum < IPEndPoint.MinPort || IPEndPoint.MaxPort < portNum)
            {
                MessageBox.Show("無効なポート番号が指定されています。");
                return false;
            }

            return true;
        }

        private void AddLog(string text)
        {
            Dispatcher.Invoke(new Action(() => 
            {
                // ログを追加
                txtLog.Text = txtLog.Text + $"{DateTime.Now.ToString("HH:mm:ss:ff")}：{text}" + "\r\n";
            }));
        }

        private async Task AsyncAcceptWaitLoop()
        {
            AddLog("接続受け入れ開始。");

            await Task.Run(() =>
            {
                // サーバーが監視中の間は接続を受け入れる
                while (Server != null && Server.Active)
                {
                    try
                    {
                        // 非同期で接続を待ち受ける
                        Server.BeginAcceptTcpClient(AcceptCallback, null).AsyncWaitHandle.WaitOne(-1);
                    }
                    catch (Exception ex)
                    {
                        AddLog("接続受け入れでエラーが発生しました。");
                        break;
                    }
                }
            });

            AddLog("接続受け入れ終了。");
        }

        /// <summary>
        /// クライアントからの接続受け入れ処理
        /// </summary>
        /// <param name="ar"></param>
        private void AcceptCallback(IAsyncResult ar) 
        {
            try
            {
                // 接続を受け入れる
                var client = Server.EndAcceptTcpClient(ar);

                // 接続ログを出力
                AddLog($"{client.Client.RemoteEndPoint}からの接続");

                // 接続中クライアントを追加
                var clientInfo = new TcpClientEx(client);
                ClientList.Add(clientInfo);
                SetConnectingClient();

                // クライアントからのデータ受信を待機
                var data = new CommunicationData(clientInfo);
                client.Client.BeginReceive(data.Data, 0, data.Data.Length, SocketFlags.None, ReceiveCallback, data);

                // 接続中クライアント（接続してきたクライアント以外）に対してクライアントが接続した情報を送信する。
                SendDataToAllClient(data, $"{data.Client.RemoteEndPoint}がサーバーに接続しました。");
            }
            catch(Exception) { }
        }

        /// <summary>
        /// 接続中クライアントへtextを送信する
        /// </summary>
        /// <param name="data">クライアント情報</param>
        /// <param name="text">送信内容</param>
        private void SendDataToAllClient(CommunicationData data, string text)
        {
            lock (ClientList.SyncRoot)
            {
                foreach (var client in ClientList.Where(e => !e.Equals(data.Client)))
                {
                    // データ送信
                    client.Socket.Send(Encoding.UTF8.GetBytes(text));

                    // 送信ログを出力
                    AddLog($"{client.RemoteEndPoint}にデータ送信 >> {text}");
                }
            }
        } 

        /// <summary>
        /// クライアントからデータを受信した際の処理
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // クライアントからのデータを受信
                var data = ar.AsyncState as CommunicationData;
                var length = data.Client.Socket.EndReceive(ar);

                // 受信データが0byteの場合切断と判定
                if(length == 0)
                {
                    // 切断ログを出力
                    AddLog($"{data.Client.RemoteEndPoint}からの切断");

                    // 接続中クライアントを削除
                    ClientList.Remove(data.Client);
                    SetConnectingClient();

                    // 接続中クライアント（切断したクライアント以外）に対して
                    // クライアントが切断した情報を発信する。
                    SendDataToAllClient(data, $"{data.Client.RemoteEndPoint}がサーバーから切断しました。");

                    // データ受信を終了
                    return;
                }

                // 受信データを出力
                AddLog($"{data.Client.RemoteEndPoint}からデータ受信 << {data}");

                // 接続中クライアント（切断したクライアント以外）に対して
                // 受信したデータを送信する。
                SendDataToAllClient(data, data.ToString());

                // サーバーが監視中の場合
                if(Server != null && Server.Active)
                {
                    // 再度クライアントからのデータ受信を待機
                    data.Client.Socket.BeginReceive(data.Data, 0, data.Data.Length, SocketFlags.None, ReceiveCallback, data);
                }
            }
            catch(Exception) { }
        }
    }
}
