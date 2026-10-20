# Unity Sharing Geospatial App
GNSSを持たないカメラのあるデバイスへGeospatial APIを用いた高精度な位置情報を提供するアプリケーション。スマートフォンと同一のネットワークで、QRを読み取って得られる接続先へリクエストを送信するとJsonで位置情報を得られる。[HoloLens 2アプリケーション](https://github.com/towakun887/bousai-hla-hololens)向けに開発。

### 技術情報
本アプリケーションへのリクエストは以下のフォーマットで`http://{QRより取得するIPアドレス}:{QRより取得するポート番号}`に対して行ってください。
```
GET_LOCATION ID:{QRより取得するデバイスID}
```
本アプリケーションのレスポンスのサンプルは以下の通りです。
```json
{
    "isTracking": true,
    "latitude": 35.45623905271915,
    "longitude": 133.36169683248121,
    "altitude": 44.9240898527205,
    "eunRotationX": 0.5096911191940308,
    "eunRotationY": -0.7153319120407104,
    "eunRotationZ": 0.3617230951786041,
    "eunRotationW": 0.3125245273113251,
    "accuracy": 18.74230295409196,
    "altitudeAccuracy": 2.2175529761389865,
    "headingAccuracy": 22.43902044057265
}
```

本ソフトウェアではGeospatial APIを利用しています。Project Settings内のARCore設定で、Geospatial APIが利用可能なARCore APIトークンを設定してください。なお、本リポジトリ内に含まれているARCore APIのトークンはすべて無効化済みです。
