
namespace MonoRaspberryPi
{
    using System;

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
