// 勤怠カード管理システム

namespace MonoRaspberryPi
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using System.Text;

    public class KintaiApplication
    {
        // 設定クラス
        private ApplicationConfig config;

        // Kintone接続クラス
        private Kintone kintone;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public KintaiApplication()
        {
            // 設定ファイル読み込み
            this.ConfigRead();

            Console.WriteLine("ID = " + this.config.id);
            Console.WriteLine("HOST = " + this.config.host);
        }

        /// <summary>
        /// Kintone接続クラス生成
        /// </summary>
        public void Init()
        {
            // 接続クラス作成
            this.kintone = new Kintone(this.config.id, this.config.password, this.config.host);
        }

        /// <summary>
        /// 処理実行
        /// </summary>
        public void Run()
        {
            // Raspberry pi GPIO制御クラス
            GpioManager nabager = new GpioManager();

            nabager.Start();
            /*
            for (;;)
            {
                // カードリーダー読み取りクラス作成
                FelicaReader reader = new FelicaReader();

                reader.Readed += ReadedHandler;
                reader.Read();
            }
            */
        }

        /// <summary>
        /// カード情報取得ハンドラー
        /// </summary>
        /// <param name="sender">イベント元</param>
        /// <param name="e">パラメーター</param>
        private void ReadedHandler(object sender, CardReadedEventArgs e)
        {
            Console.WriteLine("ID = " + e.ID);
            Console.WriteLine("PM = " + e.PM);
            Console.WriteLine("SYS = " + e.SYS);

            this.KintaiSend(e.ID);
        }

        /// <summary>
        /// 勤怠サーバー処理
        /// </summary>
        /// <param name="idm">カードID</param>
        public void KintaiSend(string idm)
        {
            if(this.kintone == null)
            {
                return;
            }
            // 打刻情報取得
            KintaiRecords result = this.kintone.ReadAttendanceRecord(idm).Result;

            if (result.IsExist)
            {
                // レコードが存在するので、休憩か退勤
                if (string.IsNullOrEmpty(result.RestStartTime))
                {
                    // 休憩開始打刻
                    KintaiResult createResult = this.kintone.RestStart(result.RecordNo).Result;
                }
                else if (result.RestStartTime == result.RestEndTime)
                {
                    // 休憩開始と休憩終了が同じなら休憩終了打刻
                    KintaiResult createResult = this.kintone.RestEnd(result.RecordNo, result.RestStartTime).Result;
                }
                else
                {
                    // 退勤打刻
                    KintaiResult createResult = this.kintone.ClockingOut(result.RecordNo).Result;
                }
            }
            else
            {
                // レコードが存在しないので、新規登録
                // 出勤打刻
                KintaiResult createResult = this.kintone.CreateAttendanceRecord(idm).Result;
            }
        }

        /// <summary>
        /// 設定ファイル読み込み
        /// </summary>
        public void ConfigRead()
        {
            using (StreamReader reader = new StreamReader("config.json", Encoding.UTF8))
            {
                string text = reader.ReadToEnd();

                reader.Close();

                using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
                {
                    stream.Position = 0;

                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ApplicationConfig));

                    this.config = (ApplicationConfig)ser.ReadObject(stream);
                }
            }
        }
    }

    /// <summary>
    /// 勤怠カード管理システムクラス
    /// </summary>
    class Program
    {
        /// <summary>
        /// メイン関数
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            if (!File.Exists("config.json"))
            {
                // 設定ファイルが無いので、空の定義ファイルを作成
                Console.WriteLine("設定ファイルのテンプレートを作成しました。");
                ConfigWrite();
                return;
            }

            KintaiApplication app = new KintaiApplication();

            if (args.Length == 1)
            {
                if (args[0] == "-test")
                {
                    // 通信テストモード
                    Console.WriteLine("テスト送信");
                    app.Init();
                    app.KintaiSend("0123456789000000");
                }
                else if(args[0] == "-read")
                {
                    // カード読み取りテストモード
                    /*
                    for (;;)
                    {
                        // カードリーダー読み取りクラス作成
                        FelicaReader reader = new FelicaReader();

                        reader.Readed += ReadedHandler;
                        reader.Read();
                    }
                    */
                    FelicaReader.OutputDataReceived = (e) =>
                    {
                        Console.WriteLine("ID = " + e.ID);
                        Console.WriteLine("PM = " + e.PM);
                        Console.WriteLine("SYS = " + e.SYS);
                    };

                    for (;;)
                    {
                        FelicaReader.ReadStatic();
                    }
                }
                else if(args[0] == "-gpio")
                {
                    GpioManager nabager = new GpioManager();

                    nabager.Start();

                    while (Console.ReadKey().KeyChar != 'q')
                    {
                        continue;
                    }
                }
            }
            else
            {
                app.Init();
                app.Run();
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
    
        static void ReadedHandler(object sender, CardReadedEventArgs e)
        {
            Console.WriteLine("ID = " + e.ID);
            Console.WriteLine("PM = " + e.PM);
            Console.WriteLine("SYS = " + e.SYS);
        }
    }
}
