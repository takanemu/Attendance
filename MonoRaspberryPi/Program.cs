// 勤怠カード管理システム

namespace MonoRaspberryPi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Timers;

    /// <summary>
    /// 勤怠カード管理システムクラス
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            GpioManager nabager = new GpioManager();

            nabager.Start();

            //while (Console.ReadKey().KeyChar != 'q')
            //{
            //    continue;
            //}

            for (; ; )
            {
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
            Console.WriteLine("ID = " + e.ID);
            Console.WriteLine("PM = " + e.PM);
            Console.WriteLine("SYS = " + e.SYS);
        }
    }

    /// <summary>
    /// GPIO管理クラス
    /// </summary>
    class GpioManager
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

            this.pi.PinMode(22, PinMode.Out);
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
    class RaspberrPi
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
    public class FelicaReader
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
    public class CardReadedEventArgs : EventArgs
    {
        public string ID { get; set; }
        public string PM { get; set; }
        public string SYS { get; set; }
    }

}
