// 勤怠カード管理システム

namespace MonoRaspberryPi
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Cache;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Security.Policy;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using System.Web;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// 勤怠カード管理システムクラス
    /// </summary>
    class Program
    {
        // 設定クラス
        private static ApplicationConfig config;

        // Kintone接続クラス
        private static Kintone kintone;

        /// <summary>
        /// メイン関数
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // 設定ファイル読み込み
            Program.ConfigRead();

            // 接続クラス作成
            kintone = new Kintone(Program.config.id, Program.config.password, Program.config.host);

            // Raspberry pi GPIO制御クラス
            GpioManager nabager = new GpioManager();

            nabager.Start();

            //while (Console.ReadKey().KeyChar != 'q')
            //{
            //    continue;
            //}

            for (; ; )
            {
                // カードリーダー読み取りクラス作成
                FelicaReader reader = new FelicaReader();

                reader.Readed += ReadedHandler;
                reader.Read();
            }
        }

        /// <summary>
        /// カード情報取得ハンドラー
        /// </summary>
        /// <param name="sender">イベント元</param>
        /// <param name="e">パラメーター</param>
        static void ReadedHandler(object sender, CardReadedEventArgs e)
        {
            //Console.WriteLine("ID = " + e.ID);
            //Console.WriteLine("PM = " + e.PM);
            //Console.WriteLine("SYS = " + e.SYS);
            string IDm = e.ID;

            // 打刻情報取得
            KintaiRecords result = kintone.ReadAttendanceRecord(IDm).Result;

            if (result.IsExist)
            {
                // レコードが存在するので、休憩か退勤
                if (string.IsNullOrEmpty(result.RestStartTime))
                {
                    // 休憩開始打刻
                    KintaiResult createResult = kintone.RestStart(result.RecordNo).Result;
                }
                else if (result.RestStartTime == result.RestEndTime)
                {
                    // 休憩開始と休憩終了が同じなら休憩終了打刻
                    KintaiResult createResult = kintone.RestEnd(result.RecordNo, result.RestStartTime).Result;
                }
                else
                {
                    // 退勤打刻
                    KintaiResult createResult = kintone.ClockingOut(result.RecordNo).Result;
                }
            }
            else
            {
                // レコードが存在しないので、新規登録
                // 出勤打刻
                KintaiResult createResult = kintone.CreateAttendanceRecord(IDm).Result;
            }
        }

        /// <summary>
        /// 設定ファイル読み込み
        /// </summary>
        static void ConfigRead()
        {
            using (StreamReader reader = new StreamReader("config.json", Encoding.UTF8))
            {
                string text = reader.ReadToEnd();

                reader.Close();

                using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
                {
                    stream.Position = 0;

                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ApplicationConfig));

                    Program.config = (ApplicationConfig)ser.ReadObject(stream);
                }
            }
        }

        /// <summary>
        /// 設定ファイル書き込み
        /// </summary>
        static void ConfigWrite()
        {
            ApplicationConfig config = new ApplicationConfig();

            MemoryStream stream1 = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ApplicationConfig));

            ser.WriteObject(stream1, config);

            stream1.Position = 0;
            StreamReader sr = new StreamReader(stream1);

            StreamWriter writer = new StreamWriter("config.json", false, Encoding.UTF8);
            writer.WriteLine(sr.ReadToEnd());
            writer.Close();
        }
    }

    /// <summary>
    /// 勤怠レコードクラス
    /// </summary>
    internal class KintaiRecords
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public KintaiRecords()
        {
            this.IsExist = false;
        }

        /// <summary>
        /// レコードの有無
        /// </summary>
        public bool IsExist { get; set; }

        /// <summary>
        /// レコード番号
        /// </summary>
        public string RecordNo { get; set; }

        /// <summary>
        /// 休憩開始時間(nullなら出勤のみ)
        /// </summary>
        public string RestStartTime { get; set; }

        /// <summary>
        /// 休憩戻り時間(休憩開始時間と同じなら休憩中)
        /// </summary>
        public string RestEndTime { get; set; }
    }

    /// <summary>
    /// 設定ファイル・エンティティ
    /// </summary>
    [DataContract]
    internal class ApplicationConfig
    {
        /// <summary>
        /// ID
        /// </summary>
        [DataMember]
        public string id;

        /// <summary>
        /// パスワード
        /// </summary>
        [DataMember]
        public string password;

        /// <summary>
        /// ホストアドレス
        /// </summary>
        [DataMember]
        public string host;
    }

    /// <summary>
    /// 出勤打刻・エンティティ
    /// </summary>
    [DataContract]
    internal class CreateKintai
    {
        /// <summary>
        /// アプリケーション番号
        /// </summary>
        [DataMember]
        public int app = 17;

        /// <summary>
        /// レコード
        /// </summary>
        [DataMember]
        public CreateKintaiRecord record; 
    }

    /// <summary>
    /// 退勤打刻・レコード
    /// </summary>
    [DataContract]
    internal class ClockingOutKintai
    {
        /// <summary>
        /// アプリケーション番号
        /// </summary>
        [DataMember]
        public int app = 17;

        /// <summary>
        /// レコード番号
        /// </summary>
        [DataMember]
        public string id;

        /// <summary>
        /// レコード
        /// </summary>
        [DataMember]
        public ClockingOutKintaiRecord record;
    }

    /// <summary>
    /// 出勤打刻レコード
    /// </summary>
    internal class CreateKintaiRecord
    {
        /// <summary>
        /// カード番号
        /// </summary>
        [DataMember(Name = "IDm")]
        public KintaiItemValue IDm { get; set; }

        /// <summary>
        /// 出勤時刻
        /// </summary>
        [DataMember(Name = "出勤時刻")]
        public KintaiItemValue 出勤時刻 { get; set; }
    }

    /// <summary>
    /// 退勤打刻レコード
    /// </summary>
    internal class ClockingOutKintaiRecord
    {
        /// <summary>
        /// 退勤打刻
        /// </summary>
        [DataMember(Name = "退勤時刻")]
        public KintaiItemValue 退勤時刻 { get; set; }
    }

    /// <summary>
    /// 勤怠データ・エンティティ
    /// </summary>
    [DataContract]
    internal class KintaiItem
    {
        /// <summary>
        /// レコード番号
        /// </summary>
        [DataMember(Name = "レコード番号")]
        public KintaiItemValue RecordNo { get; set; }
    }

    /// <summary>
    /// バリュー・エンティティ
    /// </summary>
    [DataContract]
    internal class KintaiItemValue
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="type">型</param>
        /// <param name="value">値</param>
        public KintaiItemValue(string type, string value)
        {
            this.type = type;
            this.value = value;
        }

        /// <summary>
        /// 型
        /// </summary>
        [DataMember(Name = "type")]
        public string type { get; set; }

        /// <summary>
        /// 文字列
        /// </summary>
        [DataMember(Name = "value")]
        public string value { get; set; }
    }

    /// <summary>
    /// 結果・エンティティ
    /// </summary>
    [DataContract]
    internal class KintaiResult
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public KintaiResult()
        {
            this.IsResult = true;
        }

        /// <summary>
        /// レコード番号
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// リビジョン番号
        /// </summary>
        [DataMember(Name = "revision")]
        public int Revision { get; set; }

        /// <summary>
        /// 成功フラグ
        /// </summary>
        public bool IsResult { get; set; }
    }

    /// <summary>
    /// Kintone接続クラス
    /// </summary>
    internal class Kintone
    {
        string id;
        string pass;
        string host;

        /// <summary>
        /// エンコーディング
        /// </summary>
        private Encoding enc = Encoding.GetEncoding("UTF-8");

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="pass">パスワード</param>
        /// <param name="host">ホストアドレス</param>
        public Kintone(string id, string pass, string host)
        {
            this.id = id;
            this.pass = pass;
            this.host = host;
        }

        /// <summary>
        /// 認証コード文字列生成
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="pass">パスワード</param>
        /// <returns>生成文字列</returns>
        protected string XCybozuAuthorizationBase64(string id, string pass)
        {
            return Convert.ToBase64String(enc.GetBytes(id + ":" + pass));
        }

        /// <summary>
        /// カード番号文字列生成
        /// </summary>
        /// <param name="idm">カード番号</param>
        /// <returns>生成文字列</returns>
        protected string IDmUrlEncode(string idm)
        {
            return "IDm" + Uri.EscapeUriString(" = \"" + idm + "\" ");
        }

        /// <summary>
        /// 日付文字列生成
        /// </summary>
        /// <returns>生成文字列</returns>
        protected string DateUrlEncode()
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd");

            return Uri.EscapeUriString(" 日付 = \"" + date + "\"");
        }

        /// <summary>
        /// レコード番号文字列生成
        /// </summary>
        /// <returns>生成文字列</returns>
        protected string Fields1UrlEncode()
        {
            return "&fields" + Uri.EscapeUriString("[0]=レコード番号");
        }

        /// <summary>
        /// 休憩積算文字列生成
        /// </summary>
        /// <returns>生成文字列</returns>
        protected string Fields2UrlEncode()
        {
            return "&fields" + Uri.EscapeUriString("[1]=休憩積算");
        }

        /// <summary>
        /// REST(GET)の通信の実施
        /// </summary>
        /// <returns></returns>
        protected async Task<string> ExecuteRestGet(string idm)
        {
            string resultString = "";

            UriBuilder ub = new UriBuilder("https", this.host, 443, @"k/v1/records.json", "?app=17&query=" + this.IDmUrlEncode(idm) + "and" + this.DateUrlEncode() + this.Fields1UrlEncode() + this.Fields2UrlEncode());

            var request = HttpWebRequest.CreateHttp(ub.Uri);

            HttpClientHandler handler = new HttpClientHandler();

            HttpClient cl = new HttpClient(handler);
            cl.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            cl.DefaultRequestHeaders.Add("X-Cybozu-Authorization", this.XCybozuAuthorizationBase64(this.id, this.pass));
            cl.DefaultRequestHeaders.Add("Host", ub.Host + ":" + ub.Port);

            HttpResponseMessage httpResponse = null;
            try
            {
                httpResponse = await cl.GetAsync(ub.Uri);
            }
            catch
            {
                throw;
            }

            // サーバエラー(404NotFoundや500InternalErrorなど)の場合は、WebExceptionをスローする
            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new WebException(httpResponse.ReasonPhrase.ToString());
            }

            resultString = await httpResponse.Content.ReadAsStringAsync();

            return resultString;
        }

        /// <summary>
        /// 指定レコード情報の取得
        /// </summary>
        /// <param name="idm">カード番号</param>
        /// <returns>レコード情報</returns>
        public async Task<KintaiRecords> ReadAttendanceRecord(string idm)
        {
            string result = await this.ExecuteRestGet(idm);

            KintaiRecords entity = new KintaiRecords();

            try
            {
                dynamic resultJson = JValue.Parse(result);
                JArray array = resultJson.records;

                if (array.Count > 0)
                {
                    // レコードが存在するので、休憩か退勤
                    entity.IsExist = true;

                    JObject theItem = (JObject)array.First;
                    KintaiItem theData = theItem.ToObject<KintaiItem>();

                    entity.RecordNo = theData.RecordNo.value;

                    JArray sekisanArray = (JArray)theItem["休憩積算"]["value"];

                    if (sekisanArray.Count > 0)
                    {
                        JObject theSekisanObject = (JObject)sekisanArray[0]["value"];

                        entity.RestStartTime = theSekisanObject["休憩開始"].ToObject<KintaiItemValue>().value;
                        entity.RestEndTime = theSekisanObject["休憩終了"].ToObject<KintaiItemValue>().value;
                    }
                }
            }
            catch (Exception ex)
            {
                Exception exception = new Exception(ex.Message);
                throw exception;
            }
            return entity;
        }

        /// <summary>
        /// 出勤打刻用JSON作成
        /// </summary>
        /// <param name="idm">カード番号</param>
        /// <returns>JSON文字列</returns>
        protected string MakeCreateRecord(string idm)
        {
            CreateKintai entity = new CreateKintai();

            entity.record = new CreateKintaiRecord();

            entity.record.IDm = new KintaiItemValue("SINGLE_LINE_TEXT", idm);
            entity.record.出勤時刻 = new KintaiItemValue("TIME", DateTime.Now.ToString("HH:mm"));

            string json = JsonConvert.SerializeObject(entity);

            return json;
        }

        /// <summary>
        /// 出勤打刻
        /// </summary>
        /// <param name="idm">カード番号</param>
        /// <returns>結果</returns>
        public async Task<KintaiResult> CreateAttendanceRecord(string idm)
        {
            UriBuilder ub = new UriBuilder("https", this.host, 443, @"k/v1/record.json");

            var request = HttpWebRequest.CreateHttp(ub.Uri);

            HttpClientHandler handler = new HttpClientHandler();

            HttpClient cl = new HttpClient(handler);
            cl.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            cl.DefaultRequestHeaders.Add("X-Cybozu-Authorization", this.XCybozuAuthorizationBase64(this.id, this.pass));
            cl.DefaultRequestHeaders.Add("Host", ub.Host + ":" + ub.Port);

            string json = this.MakeCreateRecord(idm);

            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage httpResponse = null;
            try
            {
                httpResponse = await cl.PostAsync(ub.Uri, content);
            }
            catch
            {
                throw;
            }
            if (!httpResponse.IsSuccessStatusCode)
            {
                KintaiResult result = new KintaiResult
                {
                    IsResult = false                     
                };
                return result;
            }
            string resultString = await httpResponse.Content.ReadAsStringAsync();

            KintaiResult resultJson = JValue.Parse(resultString).ToObject<KintaiResult>();

            return resultJson;
        }

        /// <summary>
        /// 退勤打刻用JSON作成
        /// </summary>
        /// <param name="recordNo">レコード番号</param>
        /// <returns>JSON文字列</returns>
        protected string MakeClockingOut(string recordNo)
        {
            ClockingOutKintai entity = new ClockingOutKintai();

            entity.id = recordNo;
            entity.record = new ClockingOutKintaiRecord();

            entity.record.退勤時刻 = new KintaiItemValue("TIME", DateTime.Now.ToString("HH:mm"));

            string json = JsonConvert.SerializeObject(entity);

            return json;
        }

        /// <summary>
        /// 休憩開始JSON文字列作成
        /// </summary>
        /// <param name="recordNo">レコード番号</param>
        /// <returns>JSON文字列</returns>
        protected string MakeRestStart(string recordNo)
        {
            string json = "{\"app\":17,\"id\":\"@record@\",\"record\":{\"休憩積算\":{\"value\":{\"0\":{\"value\":{\"休憩開始\":{\"value\":\"@time@\"},}},}}}}";

            json = System.Text.RegularExpressions.Regex.Replace(json, "@record@", recordNo);
            json = System.Text.RegularExpressions.Regex.Replace(json, "@time@", DateTime.Now.ToString("HH:mm"));

            return json;
        }

        /// <summary>
        /// 休憩戻りJSON文字列作成
        /// </summary>
        /// <param name="recordNo">レコード番号</param>
        /// <param name="start">休憩開始時刻</param>
        /// <returns>JSON文字列</returns>
        protected string MakeRestEnd(string recordNo, string start)
        {
            string json = "{\"app\":17,\"id\":\"@record@\",\"record\":{\"休憩積算\":{\"value\":{\"0\":{\"value\":{\"休憩戻り\":{\"value\":\"@time2@\"},\"休憩開始\":{\"value\":\"@time1@\"},}},}}}}";

            json = System.Text.RegularExpressions.Regex.Replace(json, "@record@", recordNo);
            json = System.Text.RegularExpressions.Regex.Replace(json, "@time1@", start);
            json = System.Text.RegularExpressions.Regex.Replace(json, "@time2@", DateTime.Now.ToString("HH:mm"));

            return json;
        }

        /// <summary>
        /// Put処理
        /// </summary>
        /// <param name="json">JSON文字列</param>
        /// <returns>結果</returns>
        protected async Task<KintaiResult> KintaiPutAsync(string json)
        {
            UriBuilder ub = new UriBuilder("https", this.host, 443, @"k/v1/record.json");

            var request = HttpWebRequest.CreateHttp(ub.Uri);

            HttpClientHandler handler = new HttpClientHandler();

            HttpClient cl = new HttpClient(handler);
            cl.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            cl.DefaultRequestHeaders.Add("X-Cybozu-Authorization", this.XCybozuAuthorizationBase64(this.id, this.pass));
            cl.DefaultRequestHeaders.Add("Host", ub.Host + ":" + ub.Port);

            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage httpResponse = null;
            try
            {
                httpResponse = await cl.PutAsync(ub.Uri, content);
            }
            catch
            {
                throw;
            }
            if (!httpResponse.IsSuccessStatusCode)
            {
                KintaiResult result = new KintaiResult
                {
                    IsResult = false
                };
                return result;
            }
            string resultString = await httpResponse.Content.ReadAsStringAsync();

            KintaiResult resultJson = JValue.Parse(resultString).ToObject<KintaiResult>();

            return resultJson;
        }

        /// <summary>
        /// 休憩開始打刻
        /// </summary>
        /// <param name="recordNo">レコード番号</param>
        /// <returns>結果</returns>
        public async Task<KintaiResult> RestStart(string recordNo)
        {
            string json = this.MakeRestStart(recordNo);

            KintaiResult result = await KintaiPutAsync(json);

            return result;
        }

        /// <summary>
        /// 休憩戻り打刻
        /// </summary>
        /// <param name="recordNo">レコード番号</param>
        /// <param name="start"></param>
        /// <returns>結果</returns>
        public async Task<KintaiResult> RestEnd(string recordNo, string start)
        {
            string json = this.MakeRestEnd(recordNo, start);

            KintaiResult result = await KintaiPutAsync(json);

            return result;
        }

        /// <summary>
        /// 退勤打刻
        /// </summary>
        /// <param name="recordNo">レコード番号</param>
        /// <returns>結果</returns>
        public async Task<KintaiResult> ClockingOut(string recordNo)
        {
            string json = this.MakeClockingOut(recordNo);

            KintaiResult result = await KintaiPutAsync(json);

            return result;
        }

    }

    /// <summary>
    /// GPIO管理クラス
    /// </summary>
    internal class GpioManager
    {
        private RaspberrPi pi;
        private System.Timers.Timer myTimer;
        private int button = 0;
        private int led = 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public GpioManager()
        {
            this.myTimer = new System.Timers.Timer();
            this.myTimer.Enabled = true;
            this.myTimer.AutoReset = true;
            this.myTimer.Interval = 100;
            this.myTimer.Elapsed += new ElapsedEventHandler(OnTimerEvent);
        }

        /// <summary>
        /// 処理開始
        /// </summary>
        public void Start()
        {
            this.pi = new RaspberrPi();

            this.pi.PinMode(18, PinMode.Out);
            this.pi.PinMode(23, PinMode.Out);
            this.pi.PinMode(24, PinMode.In);
            this.pi.PinMode(25, PinMode.Out);

            this.LedOn(this.led);

            this.StartTimer();
        }

        private void OnTimerEvent(object source, ElapsedEventArgs e)
        {
            int sw = this.pi.PinRead(24);

            if(sw != this.button)
            {
                if (sw == 1)
                {
                    this.led++;
                    this.led = this.led > 2 ? 0 : this.led;
                    this.LedOn(this.led);
                }
                this.button = sw;
            }
        }

        private void LedOn(int no)
        {
            switch(no)
            {
                case 0:
                    this.pi.PinWrite(22, 0);
                    this.pi.PinWrite(23, 0);
                    this.pi.PinWrite(25, 1);
                    break;
                case 1:
                    this.pi.PinWrite(22, 0);
                    this.pi.PinWrite(23, 1);
                    this.pi.PinWrite(25, 0);
                    break;
                case 2:
                    this.pi.PinWrite(22, 1);
                    this.pi.PinWrite(23, 0);
                    this.pi.PinWrite(25, 0);
                    break;
            }
        }

        public void StartTimer()
        {
            myTimer.Start();
        }

        public void StopTimer()
        {
            myTimer.Stop();
        }

    }

    /// <summary>
    /// ピンモード
    /// </summary>
    enum PinMode
    {
        In,
        Out,
    }

    /// <summary>
    /// ラズベリーパイクラス
    /// </summary>
    internal class RaspberrPi
    {
        /// <summary>
        /// GPIOフラグバッファ
        /// </summary>
        private int[] gpio = new int[26];

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public RaspberrPi()
        {
            for (int i = 0; i < this.gpio.Length; i++)
            {
                gpio[i] = 0;
            }
        }

        /// <summary>
        /// ピンモードの設定
        /// </summary>
        /// <param name="no">ピン番号</param>
        /// <param name="mode">モード</param>
        public void PinMode(int no, PinMode mode)
        {
            this.ProcessExec("-g mode " + no + " " + (mode == MonoRaspberryPi.PinMode.In ? "in" : "out"));
        }

        /// <summary>
        /// ピン書き込み
        /// </summary>
        /// <param name="no">ピン番号</param>
        /// <param name="value">値</param>
        public void PinWrite(int no, int value)
        {
            if(this.gpio[no-1] != value)
            {
                this.ProcessExec("-g write " + no + " " + value);
                this.gpio[no-1] = value;
            }
        }

        /// <summary>
        /// ピン読み取り
        /// </summary>
        /// <param name="no">ピン番号</param>
        /// <returns>値</returns>
        public int PinRead(int no)
        {
            string result = this.ProcessExec("-g read " + no);

            return int.Parse(result.Trim());
        }

        /// <summary>
        /// 外部プロセス実行
        /// </summary>
        /// <param name="param">パラメーター</param>
        /// <returns>標準出力</returns>
        public string ProcessExec(string param)
        {
            //Processオブジェクトを作成
            System.Diagnostics.Process p = new System.Diagnostics.Process();

            p.StartInfo.FileName = "gpio";
            //出力を読み取れるようにする
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = false;
            //ウィンドウを表示しないようにする
            p.StartInfo.CreateNoWindow = true;
            //コマンドラインを指定
            p.StartInfo.Arguments = param;

            //起動
            p.Start();

            //出力を読み取る
            string results = p.StandardOutput.ReadToEnd();

            //プロセス終了まで待機する
            p.WaitForExit();
            p.Close();

            return results;
        }
    }

    /// <summary>
    /// フェリカリーダークラス
    /// </summary>
    internal class FelicaReader
    {
        /// <summary>
        /// イベント
        /// </summary>
        /// <param name="sender">イベント元</param>
        /// <param name="e">パラメーター</param>
        public delegate void CardReadedEventHandler(object sender, CardReadedEventArgs e);

        /// <summary>
        /// イベント
        /// </summary>
        public event CardReadedEventHandler Readed;

        /// <summary>
        /// 読み取り開始
        /// </summary>
        public void Read()
        {
            //  プロセスオブジェクトを生成
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            //  実行ファイルを指定
            p.StartInfo.FileName = @"/home/pi/nfcpy-0.9.1/examples/tagtool.py";
            //  シェルコマンドを無効に
            p.StartInfo.UseShellExecute = false;
            //  入力をリダイレクト
            p.StartInfo.RedirectStandardInput = true;
            //  出力をリダイレクト
            p.StartInfo.RedirectStandardOutput = true;
            //  OutputDataReceivedイベントハンドラを追加
            p.OutputDataReceived += OutputDataReceivedHandler;
            //  プロセスを実行
            p.Start();
            //  非同期で出力の読み取りを開始
            p.BeginOutputReadLine();
            //  入力を行う為のストリームライターとプロセスの標準入力をリンク
            System.IO.StreamWriter myStreamWriter = p.StandardInput;

            myStreamWriter.Close();
            p.WaitForExit();
            p.Close();
        }

        /// <summary>
        /// 読み取りイベントハンドラー
        /// </summary>
        /// <param name="sender">イベント元</param>
        /// <param name="e">パラメーター</param>
        private void OutputDataReceivedHandler(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            //Console.WriteLine("FelicaReader::OutputDataReceivedHandler()");
            CardReadedEventArgs args = new CardReadedEventArgs();

            args.ID = this.GetID(e.Data);
            args.PM = this.GetPM(e.Data);
            args.SYS = this.GetSYS(e.Data);

            this.Readed(this, args);
        }

        /// <summary>
        /// 文字列解析
        /// </summary>
        /// <param name="source">解析元文字</param>
        /// <param name="regular">正規表現</param>
        /// <param name="startIndex">抽出開始インデックス</param>
        /// <param name="length">抽出文字長</param>
        /// <returns>抽出文字</returns>
        private string StringAnalyze(string source, string regular, int startIndex, int length)
        {
            System.Text.RegularExpressions.MatchCollection mc = System.Text.RegularExpressions.Regex.Matches(source, regular);

            if (mc.Count > 0)
            {
                string one = mc[0].Value;
                return one.Substring(startIndex, length);
            }
            return string.Empty;
        }

        /// <summary>
        /// ID抽出
        /// </summary>
        /// <param name="source">解析元文字</param>
        /// <returns>抽出文字</returns>
        private string GetID(string source)
        {
            return this.StringAnalyze(source, @"IDm=[0-9]*", 4, 16);
        }

        /// <summary>
        /// PM抽出
        /// </summary>
        /// <param name="source">解析元文字</param>
        /// <returns>抽出文字</returns>
        private string GetPM(string source)
        {
            return this.StringAnalyze(source, @"PMm=[0-9a-f]*", 4, 16);
        }

        /// <summary>
        /// SYS抽出
        /// </summary>
        /// <param name="source">解析元文字</param>
        /// <returns>抽出文字</returns>
        private string GetSYS(string source)
        {
            return this.StringAnalyze(source, @"SYS=[0-9]*", 4, 4);
        }
    }

    /// <summary>
    /// カード情報イベント情報
    /// </summary>
    internal class CardReadedEventArgs : EventArgs
    {
        public string ID { get; set; }
        public string PM { get; set; }
        public string SYS { get; set; }
    }

}
