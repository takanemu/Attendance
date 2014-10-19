
namespace MonoRaspberryPi
{
    using System.Timers;

    /// <summary>
    /// GPIO管理クラス
    /// </summary>
    public class GpioManager
    {
        private RaspberrPi pi;
        private System.Timers.Timer myTimer;
        private int button = 0;
        private int led = 0;

        /// <summary>
        /// 0...マニュアルモード / 1...プログレスモード
        /// </summary>
        private int mode = 0;

        private int backup;

        private readonly int LED1 = 18;
        private readonly int LED2 = 23;
        private readonly int LED3 = 25;
        private readonly int BUTTON = 24;

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
        /// LED No
        /// </summary>
        public int LEDNo
        {
            get { return this.led; }
        }

        /// <summary>
        /// Mode
        /// </summary>
        public int Mode
        {
            get
            {
                return this.mode;
            }

            set
            {
                if (this.mode == 0 && value == 1)
                {
                    this.backup = this.led;
                    this.led = 0;
                    this.LedOn(this.led);
                }
                else if(this.mode == 1 && value == 0)
                {
                    this.led = this.backup;
                    this.LedOn(this.led);
                }
                this.mode = value;
            }
        }

        /// <summary>
        /// 処理開始
        /// </summary>
        public void Start()
        {
            this.pi = new RaspberrPi();

            this.pi.PinMode(this.LED1, PinMode.Out);
            this.pi.PinMode(this.LED2, PinMode.Out);
            this.pi.PinMode(this.LED3, PinMode.Out);
            this.pi.PinMode(this.BUTTON, PinMode.In);

            this.LedOn(this.led);

            this.StartTimer();
        }

        private void OnTimerEvent(object source, ElapsedEventArgs e)
        {
            if(this.mode == 0)
            {
                // マニュアルモード
                int sw = this.pi.PinRead(this.BUTTON);

                if (sw != this.button)
                {
                    if (sw == 1)
                    {
                        this.led++;
                        this.led = this.led > 3 ? 0 : this.led;
                        this.LedOn(this.led);
                    }
                    this.button = sw;
                }
            }
            else
            {
                // プログレスモード
                this.led++;
                this.led = this.led > 3 ? 0 : this.led;
                this.LedOn(this.led);
            }
        }

        private void LedOn(int no)
        {
            switch (no)
            {
                case 0:
                    this.pi.PinWrite(this.LED1, 0);
                    this.pi.PinWrite(this.LED2, 0);
                    this.pi.PinWrite(this.LED3, 0);
                    break;
                case 1:
                    this.pi.PinWrite(this.LED1, 0);
                    this.pi.PinWrite(this.LED2, 0);
                    this.pi.PinWrite(this.LED3, 1);
                    break;
                case 2:
                    this.pi.PinWrite(this.LED1, 0);
                    this.pi.PinWrite(this.LED2, 1);
                    this.pi.PinWrite(this.LED3, 0);
                    break;
                case 3:
                    this.pi.PinWrite(this.LED1, 1);
                    this.pi.PinWrite(this.LED2, 0);
                    this.pi.PinWrite(this.LED3, 0);
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
            if (this.gpio[no - 1] != value)
            {
                this.ProcessExec("-g write " + no + " " + value);
                this.gpio[no - 1] = value;
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
}
