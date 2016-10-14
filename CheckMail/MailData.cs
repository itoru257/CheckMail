/*************************************************
* Copyright (c) 2016 Toru Ito
* Released under the MIT license
* http://opensource.org/licenses/mit-license.php
*************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CheckMail
{
    public class MailData
    {
        public string Source  = "";

        public string Date          = "";
        public string DateOrignal   = "";
        public string From          = "";
        public string To            = "";
        public string Cc            = "";
        public string Subject       = "";
        public int    Size          = 0;

        public List<string> Message = new List<string>();

        public MailData()
        {
        }

        public MailData( string Source )
        {
            this.Source = Source;
        }

        public bool IsSameMail( MailData mail )
        {
            if( this.Subject != mail.Subject )
                return false;

            if( this.DateOrignal != mail.DateOrignal )
                return false;

            if( this.From != mail.From )
                return false;

            if( this.To != mail.To )
                return false;

            if( this.Cc != mail.Cc )
                return false;

            if( this.Size != mail.Size )
                return false;

            return true;
        }

        public bool WriteSource( string fname )
        {
            List<string> Lines = new List<string>();
            string  item;
            int     pos = 0, pos_new;

            while( pos < this.Source.Length )
            {
                pos_new = this.Source.IndexOf( "\r\n", pos );
                item = this.Source.Substring( pos, pos_new - pos );

                Lines.Add( item );

                pos = pos_new + 2;
            }

            return HelpLib.WriteFileLine( fname, Lines );
        }


        public bool ReadSource( string fname )
        {
            List < string > Lines = new List<string>();

            if( !HelpLib.ReadFileLine( fname, ref Lines ) )
                return false;

            string data = "";

            foreach( string line in Lines )
            {
                data += line;
                data += "\r\n";
            }

            this.Source = data;

            return true;
        }

        public bool ConvertSourceToData()
        {
            try
            {
                bool    header_flg = true;
                string  item, key = "", key2 = "", value = "";
                int     pos = 0, pos_new, pos_item;

                string  charset = "";

                while( pos < this.Source.Length )
                {
                    pos_new = this.Source.IndexOf( "\r\n", pos );
                    item = this.Source.Substring( pos, pos_new - pos );
                    pos = pos_new + 2;

                    if( header_flg && item.Length == 0 )
                    {
                        header_flg = false;
                        continue;
                    }

                    if( header_flg )
                    {
                        if( item[0] == ' ' || item[0] == '\t' )
                        {
                            value = item.TrimStart();
                        }
                        else
                        {
                            if( (pos_item = item.IndexOf( ":" )) > 0 )
                            {
                                key = item.Substring( 0, pos_item ).ToLower();
                                key2 = "";

                                if( item.Length <= pos_item + 2 )
                                {
                                    value = "";
                                    continue;
                                }

                                value = item.Substring( pos_item + 2 );

                                if( key == "content-type" )
                                {
                                    if( (pos_item = value.IndexOf( ";" )) <= 0 )
                                        continue;

                                    key2 = value.Substring( 0, pos_item );
                                    value = value.Substring( pos_item + 1 );
                                }
                            }
                            else
                            {
                                value = item;
                            }
                        }

                        if( key == "date" )
                        {
                            this.DateOrignal = value;
                            this.Date = ConvertStringDate( value );
                        }
                        else if( key == "from" )
                        {
                            DecodeHeader( ref value );
                            this.From += value;
                        }
                        else if( key == "to" )
                        {
                            DecodeHeader( ref value );
                            this.To += value;
                        }
                        else if( key == "cc" )
                        {
                            DecodeHeader( ref value );
                            this.Cc += value;
                        }
                        else if( key == "subject" )
                        {
                            DecodeHeader( ref value );
                            this.Subject += value;
                        }
                        else if( key == "content-type" )
                        {
                            if( key2 == "text/html" || key2 == "text/plain" )
                            {
                                string[] item2 = value.Split(';');

                                foreach( string text in item2 )
                                {
                                    if( text.IndexOf( "charset" ) >= 0 )
                                    {
                                        pos_item = text.IndexOf( "=" );
                                        charset = text.Substring( pos_item + 1 );
                                        charset = charset.Replace( "\"", "" );
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        break;

                        //if( charset.Length > 0 )
                        //{
                        //    byte[] data = Encoding.ASCII.GetBytes(item.ToCharArray());
                        //    item = Encoding.GetEncoding( charset ).GetString( data, 0, data.Length );
                        //}

                        //this.Message.Add( item );
                    }
                }

                this.Size = this.Source.ToCharArray().Length;
           }
           catch
           {
               return false;
           }

            return true;
        }

        private static bool DecodeHeader( ref string data )
        {
            string[] splits = new string[]{ "?", "?", "?=" };
            string[] items = new string[3];
            string result = "";
            int i;

            int pos = 0, pos_new;

            while( pos < data.Length )
            {
                if( (pos_new = data.IndexOf( "=?", pos )) < 0 )
                {
                    result += data.Substring( pos, data.Length - pos );
                    break;
                }

                if( pos_new - pos > 0 )
                {
                    result += data.Substring( pos, pos_new - pos );
                }

                pos = pos_new + 2;

                for( i = 0; i < 3; ++i )
                {
                    if( (pos_new = data.IndexOf( splits[i], pos )) < 0 )
                        return false;

                    items[i] = data.Substring( pos, pos_new - pos );
                    pos = pos_new + splits[i].Length;
                }

                if( items[1] == "B" || items[1] == "b" )
                {
                    byte[] b = System.Convert.FromBase64String( items[2] );
                    result += System.Text.Encoding.GetEncoding( items[0] ).GetString( b );
                }
                else if( items[1] == "Q" || items[1] == "q" )
                {
                    result += DecodeQuotedPrintable( items[2], items[0] );
                }
                else
                {
                    return false;
                }
            }

            data = result;

            return true;
        }

        private static string DecodeQuotedPrintable( string input, string charset )
        {
            List<byte> data = new List<byte>();
            string code;
            int pos0 = 0;
            int pos1 = 0;

            while( pos0 < input.Length )
            {
                pos1 = input.IndexOf( "=", pos0 );

                if( pos1 < 0 || pos1 + 3 > input.Length )
                {
                    data.AddRange( Encoding.ASCII.GetBytes( input.Substring( pos0 ) ) );
                    break;
                }

                if( pos1 != pos0 )
                {
                    data.AddRange( Encoding.ASCII.GetBytes( input.Substring( pos0, pos1 - pos0 ) ) );
                }

                code = input.Substring( pos1 + 1, 2 );

                if( Uri.IsHexDigit( code[0] ) && Uri.IsHexDigit( code[1] ) )
                {
                    data.Add( (byte)Convert.ToInt32( code, 16 ) );
                    pos0 = pos1 + 3;
                }
                else
                {
                    data.Add( (byte)input[pos1] );
                    pos0 = pos1 + 1;
                }
            }

            return Encoding.GetEncoding( charset ).GetString( data.ToArray() );
        }

        private static string DecodeQuotedPrintable_utf8( string input )
        {
            var occurences = new Regex(@"=[0-9A-H]{2}", RegexOptions.Multiline);
            var matches = occurences.Matches(input);

            foreach( Match match in matches )
            {
                string text = match.Groups[0].Value.Replace( "=", "%" );
                input = input.Replace( match.Groups[0].Value, text );
            }

            return Uri.UnescapeDataString( input );
        }

        private static string ConvertStringDate( string input )
        {
            string result, input2;
            int pos;

            input2 = input;

            try
            {
                if( (pos = input2.IndexOf( "(" )) > 0 )
                {
                    input2 = input2.Substring( 0, pos );
                    input2 = input2.Trim();
                }

                if( (pos = input2.IndexOf( "," )) > 0 )
                {
                    input2 = input2.Substring( pos + 1 );
                    input2 = input2.Trim();
                }

                string[] expectedFormats = { "d MMM yyyy HH':'mm':'ss zzz", "r" };
                DateTime dt = System.DateTime.ParseExact(input2, expectedFormats,
                                        System.Globalization.DateTimeFormatInfo.InvariantInfo,
                                        System.Globalization.DateTimeStyles.None );

                result = dt.ToString( "yyyy/MM/dd ddd HH:mm" );
            }
            catch
            {
                result = input;
            }

            return result;
        }

        public string GetStringSize()
        {
            if( this.Size < 1024 )
            {
                return string.Format( "{0} B", this.Size );
            }
            else if( this.Size < 1024 * 1024 )
            {
                return string.Format( "{0:F1} KB", (double)this.Size / (double)( 1024 ) );
            }
            else if( this.Size < 1024 * 1024 * 1024 )
            {
                return string.Format( "{0:F1} MB", (double)this.Size / (double)(1024 * 1024) );
            }
            else
            {
                return string.Format( "{0:F1} GB", (double)this.Size / (double)(1024 * 1024 * 1024) );
            }

        }
    }
}
