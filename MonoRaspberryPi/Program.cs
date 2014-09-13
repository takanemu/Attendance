using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace MonoRaspberryPi
{
    class Program
    {
        static bool cardRead = true;

        static void Main(string[] args)
        {
            GpioManager nabager = new GpioManager();

            nabager.Start();

            while (Console.ReadKey().KeyChar != 'q')
            {
                if(cardRead)
                {
                    FelicaReader reader = new FelicaReader();

                    reader.Readed += ReadedHandler;
                    reader.Read();
                    cardRead = false;
                }
                continue;
            }
        }

        static void ReadedHandler(object sender, CardReadedEventArgs e)
        {
            Console.WriteLine("Read!!");
            Console.WriteLine("\"" + e.ID + "\"");
            cardRead = true;
        }
    }

    class GpioManager
    {
        private RaspberrPi pi;
        private System.Timers.Timer myTimer;
        private int button = 0;
        private int led = 0;

        public GpioManager()
        {
            this.myTimer = new System.Timers.Timer();
            this.myTimer.Enabled = true;
            this.myTimer.AutoReset = true;
            this.myTimer.Interval = 200;
            this.myTimer.Elapsed += new ElapsedEventHandler(OnTimerEvent);
        }

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
                this.button = sw;
                this.led++;
                this.led = this.led > 2 ? 0 : this.led;
                this.LedOn(this.led);
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
            this.ProcessExec("-g write " + no + " " + value);
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

    public class FelicaReader
    {
        public delegate void CardReadedEventHandler(object sender, CardReadedEventArgs e);

        public event CardReadedEventHandler Readed;

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

        private void OutputDataReceivedHandler(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            string str = e.Data;

            CardReadedEventArgs args = new CardReadedEventArgs();
            
            args.ID = str;

            this.Readed(this, args);
        }
    }

    public class CardReadedEventArgs : EventArgs
    {
        public string ID { get; set; }
    }

}
