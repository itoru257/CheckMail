/*************************************************
* Copyright (c) 2017 Toru Ito
* Released under the MIT license
* http://opensource.org/licenses/mit-license.php
*************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;

namespace CheckMail
{
    public class MailReceivePop3
    {
        public string   HostName    = "";
        public int      PortNumber  = 110;
        public string   UserId      = "";
        public string   PassWord    = "";
        public List<MailData> Mails;

        public MailReceivePop3()
        {
            Mails = new List<MailData>();
        }

        public bool Receive()
        {
            NetworkStream stream = null;
            TcpClient client = new TcpClient();
            string message = "";
            bool result = true;

            client.ReceiveTimeout = 10000;
            client.SendTimeout = 10000;

            try
            {
                client.Connect( this.HostName, this.PortNumber );
                stream = client.GetStream();

                message = ReceiveData( stream );

                SendData( stream, "USER " + this.UserId + "\r\n" );
                message = ReceiveData( stream );

                SendData( stream, "PASS " + this.PassWord + "\r\n" );
                message = ReceiveData( stream );

                SendData( stream, "STAT\r\n" );
                message = ReceiveData( stream );

                int i, numMail;

                numMail = int.Parse(message.Split(' ')[1]);
                numMail = (numMail > 50) ? 50 : numMail;

                for( i = 1; i <= numMail; ++i )
                {
                    SendData( stream, "RETR " + i.ToString() + "\r\n" );
                    message = ReceiveMultiLineData( stream, true );

                    MailData mail = new MailData(message);
                    if( mail.ConvertSourceToData() )
                    {
                        this.Mails.Add( mail );
                    }
                }

                SendData( stream, "QUIT\r\n" );
                message = ReceiveData( stream );
            }
            catch
            {
                result = false;
            }
            finally
            {
                if( stream != null )
                {
                    stream.Close();
                }

                client.Close();
            }

            return result;
        }

        private static string ReceiveMultiLineData( NetworkStream stream, bool ReceiveByteFlg = false )
        {
            byte[] data = new byte[1024];
            String responseData = String.Empty;

            if( ReceiveByteFlg )
            {
                bool InitReceiveByte = true;
                Int32 ReceiveByteTotal = 0, ReceiveByteSize = 0;
                int pos;

                do
                {
                    Int32 bytes = stream.Read( data, 0, data.Length );

                    if( bytes > 0 )
                    {
                        responseData += Encoding.ASCII.GetString( data, 0, bytes );
                        ReceiveByteTotal += bytes;

                        if( InitReceiveByte )
                        {
                            InitReceiveByte = false;

                            if( !responseData.StartsWith( "+OK" ) || ( pos = responseData.IndexOf( "\r\n" ) ) < 0 )
                            {
                                throw new Exception( "Received Error" );
                            }

                            string message = responseData.Substring( 0, pos );
                            ReceiveByteSize = int.Parse( message.Split( ' ' )[1] );
                            ReceiveByteSize += pos + 5;
                        }
                    }
                    else
                    {
                        throw new Exception( "Read Error." );
                    }

                } while( ReceiveByteTotal < ReceiveByteSize );

                //Console.WriteLine( "Received bytes: {0}", ReceiveByteTotal );
            }
            else
            {
                do
                {
                    Int32 bytes = stream.Read( data, 0, data.Length );

                    if( bytes > 0 )
                    {
                        responseData += Encoding.ASCII.GetString( data, 0, bytes );
                    }
                    else
                    {
                        throw new Exception( "Read Error." );
                    }

                } while( responseData.EndsWith( "\r\n" + "." + "\r\n" ) == false );
            }

            //Console.WriteLine( "Received: {0}", responseData );

            if( !responseData.StartsWith( "+OK" ) )
            {
                throw new Exception( "Received Error" );
            }

            return responseData;
        }

        private static string ReceiveData( NetworkStream stream )
        {
            byte[] data = new byte[256];
            String responseData = String.Empty;

            Int32 bytes = stream.Read(data, 0, data.Length);
            responseData = Encoding.ASCII.GetString( data, 0, bytes );

            //Console.WriteLine( "Received: {0}", responseData );

            if( !responseData.StartsWith( "+OK" ) )
            {
                throw new Exception( "Received Error" );
            }

            return responseData;
        }

        private static void SendData( NetworkStream stream, string message )
        {
            byte[] data = Encoding.ASCII.GetBytes( message );
            stream.Write( data, 0, data.Length );

            //Console.WriteLine( "Sent: {0}", message );
        }
    }
}
