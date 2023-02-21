# ChatApp
TCP/IPでチャットアプリを作成する。

下記サイト丸パクリなので作り方解説等はこちらをご覧ください。
- [【C#】TCP/IPでチャットアプリを作成する方法を解説【前編】](https://gorihei.com/programming/693/)
-  [【C#】TCP/IPでチャットアプリを作成する方法を解説【後編】](https://gorihei.com/programming/759/)

## 【変更点】
- WindowsFormsではなく、WPFで作成する。
- メッセージ表示部をラベルではなく、TextBoxを非活性にする。
- サーバー側のReceiveCallback()にて、全クライアントにメッセージを送信した後、CallBackする前にbyteデータを初期化する。
- クライアント側の活性/非活性の制御

## 【作成物】

![image](https://user-images.githubusercontent.com/76488848/220226600-71f578db-003c-4913-9a0d-1cbe7a4f3692.png)

### サーバー側 ／ クライアント側レイアウト

<img src="https://user-images.githubusercontent.com/76488848/220227960-4e2a0198-ff17-4cda-9f6c-de26739e5d3a.png" width="49%"> <img src="https://user-images.githubusercontent.com/76488848/220227508-4522ef25-cded-4764-a9da-cc1d6f25c67a.png" width="50%">

### 【作成時メモ】
[ソケット通信を使用したアプリを作成してみる](https://scrapbox.io/cotoros-note/%E3%82%BD%E3%82%B1%E3%83%83%E3%83%88%E9%80%9A%E4%BF%A1%E3%82%92%E4%BD%BF%E7%94%A8%E3%81%97%E3%81%9F%E3%82%A2%E3%83%97%E3%83%AA%E3%82%92%E4%BD%9C%E6%88%90%E3%81%97%E3%81%A6%E3%81%BF%E3%82%8B)

