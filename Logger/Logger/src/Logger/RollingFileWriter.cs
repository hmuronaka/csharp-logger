using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace h.mu.Logger
{
    /// <summary>
    /// 循環的にファイルに書き込むためのクラス.
    /// 
    /// １ファイルあたりのサイズ、総ファイル数はコンストラクタにて指定する。
    /// 書き込み先ファイルが1ファイルあたりのサイズに達すると、次のファイルに切り替えて書き込む。
    /// 
    /// 総ファイル数３，ファイル名にlog、拡張子にtxtを指定した場合は
    /// log0.txt
    /// log1.txt
    /// log2.txt
    /// log.index(管理ファイル）
    /// を生成する。
    /// 
    /// idxファイルは書き込み先を保持するために生成する.
    /// 
    /// ファイルは書き込む先のファイル本体と書き込み先を
    /// </summary>
    public class RollingFileWriter : IDisposable
    {
        #region プロパティ
        /// <summary>
        /// ファイルの保存先パス
        /// </summary>
        public String directory
        {
            private set;
            get;
        }

        /// <summary>
        /// ファイル名
        /// </summary>
        public String filename
        {
            private set;
            get;
        }

        /// <summary>
        /// 拡張子
        /// </summary>
        public String extension
        {
            private set;
            get;
        }

        /// <summary>
        /// 1ファイルあたりのファイルサイズ(byte)
        /// 1回の書き込みがこのサイズを超える場合は、次のファイルに書き込む.
        /// </summary>
        public int fileSize
        {
            private set;
            get;
        }

        /// <summary>
        /// 書き込み先ファイルインデックス.
        /// </summary>
        public int nowFileIndex
        {
            private set;
            get;
        }

        /// <summary>
        /// ファイルの循環数
        /// </summary>
        public int fileCount
        {
            private set;
            get;
        }
        #endregion

        /// <summary>
        /// 現在の書き込み先ファイルパス（ディレクトリ、拡張子を含む)を返す。
        /// </summary>
        public String nowFilePath
        {
            private set;
            get;
        }

        #region フィールド
        /// <summary>
        /// 書き込み用.
        /// </summary>
        private System.IO.TextWriter fileWriter;

        /// <summary>
        /// ファイルの循環数に応じた桁数を自動算出する
        /// 
        /// </summary>
        private String forFileNumberSuffix;
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="name"></param>
        /// <param name="ext"></param>
        /// <param name="fileSize"></param>
        /// <param name="fileCoun"></param>
        public RollingFileWriter(String dir, String name, String ext, int fileSize, int fileCount)
        {
            if (fileSize <= 0 || fileCount <= 0)
            {
                throw new System.ArgumentException("0以下の値を指定しました。[fileSize:" + fileSize + "][fileCount:" + fileCount + "]");
            }

            this.directory = dir;
            this.filename = name;
            this.extension = ext;
            this.fileSize = fileSize;
            this.fileCount = fileCount;
            this.forFileNumberSuffix = "D" + (int)System.Math.Log10(this.fileCount);

        }

        /// <summary>
        /// プレフィックスに基づき、configファイルより設定する.
        /// dirctory: prefix + "directory"; as String
        /// filename: prefix + "filename"; as String
        /// extension:prefix + "extension"; as String
        /// fileSize: prefix + "fileSize"; as int
        /// filecount:prefix+ "fileCount"; as int
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="prefix"></param>
        public RollingFileWriter(System.Configuration.ApplicationSettingsBase setting, String prefix)
        {
            this.directory = setting[prefix + "directory"] as String;
            this.filename = setting[prefix + "filename"] as String;
            this.extension = setting[prefix + "extension"] as String;
            this.fileSize = (int)setting[prefix + "fileSize"];
            this.fileCount = (int)setting[prefix + "fileCount"];
            this.forFileNumberSuffix = "D" + (int)System.Math.Log10(this.fileCount);

            if (fileSize <= 0 || fileCount <= 0)
            {
                throw new System.ArgumentException("0以下の値を指定しました。[fileSize:" + fileSize + "][fileCount:" + fileCount + "]");
            }

        }

        /// <summary>
        /// ファイルを開く.（テキストモード、追記)
        /// 存在しない場合は、作成を試みる.
        /// ディレクトリが存在しない場合は、それの作成も試みる.
        /// 
        /// 全ての例外はcatchせずthrowする。
        /// </summary>
        public void open()
        {
            if (!System.IO.Directory.Exists(this.directory))
            {
                System.IO.Directory.CreateDirectory(this.directory);
            }

            // 書き込み先ファイル名を生成する.
            this.nowFileIndex = this.readFileIndexAndIfNotExistCreateIndexFile();
            this.nowFilePath = this.createNowFilePath();

            this.fileWriter = System.IO.File.AppendText(this.nowFilePath);
        }

        /// <summary>
        /// ファイルを閉じる。既に閉じられている場合は何もしない.
        /// </summary>
        public void close()
        {
            if (this.fileWriter != null)
            {
                this.fileWriter.Close();
                this.fileWriter = null;
            }
        }

        /// <summary>
        /// ファイルを閉じる.
        /// </summary>
        public void Dispose()
        {
            close();
        }

        /// <summary>
        /// ファイルにテキストを書き込む.
        /// ファイルサイズがオーバーする場合は、次のファイルに書き込む
        /// </summary>
        /// <param name="text"></param>
        public void write(String text)
        {
            if (this.fileWriter == null)
            {
                throw new System.IO.IOException("ファイルがopenされていません。[file:" + this.nowFilePath + "]");
            }

            // 書き込み準備（ファイルサイズに応じて、書き込み先ファイルを次のファイルに切り替える)
            this.prevWrite(text);

            // 書き込み
            this.fileWriter.Write(text);

            this.fileWriter.Flush();

        }

        /// <summary>
        /// ファイルにテキストを書き込む.(改行付き)
        /// ファイルサイズがオーバーする場合は、次のファイルに書き込む
        /// </summary>
        /// <param name="text"></param>
        public void writeLine(String text)
        {
            this.write(text + this.fileWriter.NewLine);
        }


        private void prevWrite(String text)
        {
            int len = Encoding.UTF8.GetByteCount(text);
            // ファイルサイズチェック
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(this.nowFilePath);
            if ( (fileInfo.Length  + len) > this.fileSize)
            {
                // 次のファイルに書き込む.
                this.nowFileIndex = nextIndex();
                writeIndexFile(this.nowFileIndex);

                this.close();

                this.nowFilePath = createNowFilePath();
                this.fileWriter = new System.IO.StreamWriter(this.nowFilePath, false);
            }
        }

        private String createNowFilePath()
        {
            String nowFileName = this.filename +
            this.nowFileIndex.ToString(this.forFileNumberSuffix) +
                "." +
                this.extension;

            String fullPath = System.IO.Path.Combine(this.directory, nowFileName);
            return fullPath;
        }

        #region indexファイル関係

        /// <summary>
        /// インデックスファイル名を返す.
        /// </summary>
        /// <returns></returns>
        private String getIndexFileName()
        {
            return this.filename + ".idx";
        }

        /// <summary>
        /// indexファイルより、ファイル書き込み位置を読み、それを返す。
        /// ファイルが存在しない場合は,0を返す.
        /// </summary>
        /// <returns></returns>
        private int readFileIndexAndIfNotExistCreateIndexFile()
        {
            int result = 0;

            String indexFileName = this.getIndexFileName();
            String indexFilePath = System.IO.Path.Combine(this.directory, indexFileName);
            if (System.IO.File.Exists(indexFilePath))
            {
                byte[] fileData = System.IO.File.ReadAllBytes(indexFilePath);
                if (fileData.Length != sizeof(int))
                {
                    throw new System.IO.IOException("illegal filesize[expect:" + sizeof(int) + "][read:" + fileData.Length + "]");
                }
                result = BitConverter.ToInt32(fileData, 0);
            }
            else
            {
                System.IO.File.WriteAllBytes(indexFilePath, BitConverter.GetBytes(result));
            }
            return result;
        }

        private int nextIndex()
        {
            return (this.nowFileIndex + 1) % this.fileCount;
        }

        private void writeIndexFile(int index)
        {
            String indexFileName = this.getIndexFileName();
            String indexFilePath = System.IO.Path.Combine(this.directory, indexFileName);

            System.IO.File.WriteAllBytes(indexFilePath, BitConverter.GetBytes(index));
        }
        #endregion

    }
}
