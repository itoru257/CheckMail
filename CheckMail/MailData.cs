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

        public string MessageState = "";
        public string Date = "";
        public string From = "";
        public string To = "";
        public string Cc = "";
        public string Subject = "";

        public List<string> Message = new List<string>();

        public MailData()
        {
        }

        public MailData( string Source )
        {
            this.Source = Source;
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

                    if( MessageState.Length == 0 )
                    {
                        MessageState = item;
                        continue;
                    }
                    else if( header_flg && item.Length == 0 )
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
                            if( (pos_item = item.IndexOf( ":" )) <= 0 )
                                continue;

                            key = item.Substring( 0, pos_item );
                            key2 = "";

                            if( item.Length <= pos_item + 2 )
                            {
                                value = "";
                                continue;
                            }

                            value = item.Substring( pos_item + 2 );

                            if( key == "Content-Type" )
                            {
                                if( (pos_item = value.IndexOf( ";" )) <= 0 )
                                    continue;

                                key2 = value.Substring( 0, pos_item );
                                value = value.Substring( pos_item + 1 );
                            }
                        }

                        if( key == "Date" )
                        {
                            this.Date = value;
                        }
                        else if( key == "From" )
                        {
                            DecodeHeader( ref value );
                            this.From = value;
                        }
                        else if( key == "To" )
                        {
                            DecodeHeader( ref value );
                            this.To += value;
                        }
                        else if( key == "Cc" )
                        {
                            DecodeHeader( ref value );
                            this.Cc += value;
                        }
                        else if( key == "Subject" )
                        {
                            DecodeHeader( ref value );
                            this.Subject += value;
                        }
                        else if( key == "Content-Type" )
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

                if( items[1] == "B" )
                {
                    byte[] b = System.Convert.FromBase64String( items[2] );
                    result += System.Text.Encoding.GetEncoding( items[0] ).GetString( b );
                }
                else if( items[1] == "Q" )
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
    }
}
