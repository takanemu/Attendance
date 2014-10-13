// 勤怠カード管理システム

namespace MonoRaspberryPi
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using System.Text;

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

            Console.WriteLine("ID = " + Program.config.id);
            Console.WriteLine("HOST = " + Program.config.host);

            // 接続クラス作成
            kintone = new Kintone(Program.config.id, Program.config.password, Program.config.host);

            if (args.Length == 1 && args[0] == "-test")
            {
                KintaiSend("0123456789000000");
            }
            else
            {
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
        }

        /// <summary>
        /// カード情報取得ハンドラー
        /// </summary>
        /// <param name="sender">イベント元</param>
        /// <param name="e">パラメーター</param>
        static void ReadedHandler(object sender, CardReadedEventArgs e)
        {
            Console.WriteLine("ID = " + e.ID);
            Console.WriteLine("PM = " + e.PM);
            Console.WriteLine("SYS = " + e.SYS);

            KintaiSend(e.ID);
        }

        /// <summary>
        /// 勤怠サーバー処理
        /// </summary>
        /// <param name="idm">カードID</param>
        static void KintaiSend(string idm)
        {
            // 打刻情報取得
            KintaiRecords result = kintone.ReadAttendanceRecord(idm).Result;

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
                KintaiResult createResult = kintone.CreateAttendanceRecord(idm).Result;
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
}
