/*************************************************
* Copyright (c) 2016 Toru Ito
* Released under the MIT license
* http://opensource.org/licenses/mit-license.php
*************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CheckMail
{
    static class HelpLib
    {
        /// <summary>
        /// ファイルの読み込み
        /// </summary>
        /// <param name="fname"></param>
        /// <param name="Lines"></param>
        /// <returns></returns>
        public static bool ReadFileLine( string fname, ref List<string> Lines )
        {
            try
            {
                StreamReader textReader = new StreamReader( fname, Encoding.GetEncoding( 932 ) );
                string line;

                Lines = new List<string>();

                while( (line = textReader.ReadLine()) != null )
                {
                    Lines.Add( line );
                }

                textReader.Close();
            }
            catch
            {
                return false;
            }

            return true;
        }
        /// <summary>
        /// ファイルの保存
        /// </summary>
        /// <param name="fname"></param>
        /// <param name="Lines"></param>
        /// <returns></returns>
        public static bool WriteFileLine( string fname, List<string> Lines )
        {
            try
            {
                StreamWriter textWriter = new StreamWriter( File.Open( fname, FileMode.Create ), System.Text.Encoding.GetEncoding( 932 ) );

                foreach( string line in Lines )
                {
                    textWriter.WriteLine( line );
                }

                textWriter.Close();
            }
            catch
            {
                return false;
            }

            return true;
        }

    }
}
