using ChatAppCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace ClientApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        private TcpClientEx Client { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ClientForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // クライアントを閉じる
            if(Client != null)
            {
                Client.Client.Close();
                Client.Client.Dispose();
                Client = null;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            var ipAddress = txtAddress.Text.Trim();         // 接続先IPアドレス
            var sourcePort = txtSourcePort.Text.Trim();     // 接続元ポート番号
            var destPort = txtDestPort.Text.Trim();         // 接続先ポート番号

            try
            {
                // 接続情報有効チェック
                if (!CheckConnectionSettings(destPort, sourcePort, ipAddress)) return;

                // 接続先IPEndPoint作成
                var remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), int.Parse(destPort));

                // 接続元IPEndPoint作成
                var localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), int.Parse(sourcePort));

                // クライアント作成
                Client = new TcpClientEx(localEndPoint);

                // サーバーに接続
                Client.Client.Connect(remoteEndPoint);

                // 接続ログ出力
                SetLog("サーバーに接続しました。");

                // データ受信待機開始
                var data = new CommunicationData(Client);
                Client.Socket.BeginReceive(data.Data, 0, data.Data.Length, SocketFlags.None, ReceiveCallback, data);

                // ボタンの有効状態の制御
                txtAddress.IsEnabled    = false;
                txtSourcePort.IsEnabled = false;
                txtDestPort.IsEnabled   = false;
                btnStart.IsEnabled      = false;
                btnEnd.IsEnabled        =  true;
                txtName.IsEnabled       =  true;
                txtMessage.IsEnabled    =  true;
                btnSend.IsEnabled       =  true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                // 接続ログ出力
                SetLog("サーバーに接続できませんでした。");
            }

        }

        private void btnEnd_Click(object sender, EventArgs e)
        {
            // クライアントを閉じる
            Client.Client.Close();
            Client.Client.Dispose();
            Client = null;

            // 切断ログ出力
            SetLog("サーバーから切断しました。");

            // ボタンの有効状態を設定
            txtAddress.IsEnabled    =  true;
            txtSourcePort.IsEnabled =  true;
            txtDestPort.IsEnabled   =  true;
            btnStart.IsEnabled      =  true;
            btnEnd.IsEnabled        = false;
            txtName.IsEnabled       = false;
            txtMessage.IsEnabled    = false;
            btnSend.IsEnabled       = false;

        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                // 送信データを作成
                var data = Encoding.UTF8.GetBytes(txtName.Text + " : " + txtMessage.Text);

                // サーバーにデータを送信
                //Client?.Socket.Send(data, 0, data.Length, SocketFlags.None);
                Client?.Socket.Send(data);

                // 送信ログ出力
                SetLog($"サーバーにデータ送信 >> {Encoding.UTF8.GetString(data)}");
            }
            catch(Exception)
            {
                SetLog("データ送信に失敗しました。");
            }
        }
        

        private void SetLog(string text)
        {
            Dispatcher.Invoke(new Action(() => 
            {
                // ログを追加
                txtLog.Text = txtLog.Text + $"{DateTime.Now.ToString("HH:mm:ss:ff")}：{text}" + "\r\n";

                // 末尾に移動
                txtLog.ScrollToEnd();
            }));
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // サーバーからデータを受信
                var data = ar.AsyncState as CommunicationData;
                var length = data.Client.Socket.EndReceive(ar);

                // 受信データが0byteの場合は切断と判断
                if(length == 0)
                {
                    // 切断ログ出力
                    SetLog("サーバーから切断されました。");

                    // データ受信終了
                    return;
                }

                // 受信データを出力
                SetLog($"サーバーから受信 << {data}");

                // 再度サーバーからデータ受信を待機
                data.Client.Socket.BeginReceive(data.Data, 0, data.Data.Length, SocketFlags.None, ReceiveCallback, data);
            }
            catch(Exception)
            {
                // ボタンの有効状態を設定
                Dispatcher.Invoke(new Action(() =>
                {
                    txtAddress.IsEnabled    =  true;
                    txtSourcePort.IsEnabled =  true;
                    txtDestPort.IsEnabled   =  true;
                    btnStart.IsEnabled      =  true;
                    btnEnd.IsEnabled        = false;
                    txtName.IsEnabled       = false;
                    txtMessage.IsEnabled    = false;
                    btnSend.IsEnabled       = false;

                }));
            }
        }

        private bool CheckConnectionSettings(string destPort, string sourcePort, string ipAddress)
        {
            // ポート番号空チェック
            if(string.IsNullOrEmpty(destPort) || string.IsNullOrEmpty(sourcePort))
            {
                MessageBox.Show("ポート番号が空です。");
                return false;
            }

            // ポート番号数値チェック
            if(!Regex.IsMatch(destPort, "^[0-9]+$") || !Regex.IsMatch(sourcePort, "^[0-9]+$"))
            {
                MessageBox.Show("ポート番号は数値を指定してください。");
                return false;
            }

            var destPortNum = int.Parse(destPort);
            var sourcePortNum = int.Parse(sourcePort);

            // ポート番号有効値チェック
            if(destPortNum < IPEndPoint.MinPort || IPEndPoint.MaxPort < destPortNum ||
                sourcePortNum < IPEndPoint.MinPort || IPEndPoint.MaxPort < sourcePortNum)
            {
                MessageBox.Show("無効なポート番号が指定されています。");
                return false;
            }

            // IPアドレス有効チェック
            if(!IPAddress.TryParse(ipAddress, out IPAddress _))
            {
                MessageBox.Show("無効なIPアドレスが指定されています。");
                return false;
            }

            return true;
        }
    }
}
