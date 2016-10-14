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

namespace CheckMail
{
    class Program
    {
        static void Main( string[] args )
        {
            MailReceivePop3 pop3 = new MailReceivePop3();

            pop3.HostName   = "aaa.bbb.ccc";
            pop3.PortNumber = 110;
            pop3.UserId     = "user";
            pop3.PassWord   = "password";

            if( pop3.Receive() )
            {
                foreach( MailData mail in pop3.Mails )
                {
                    Console.WriteLine( "=====" );
                    Console.WriteLine( "Subject: {0}", mail.Subject );
                    Console.WriteLine( "Date:    {0}", mail.Date );
                    Console.WriteLine( "From:    {0}", mail.From );
                    Console.WriteLine( "To:      {0}", mail.To );
                    Console.WriteLine( "Cc:      {0}", mail.Cc );
                    Console.WriteLine( "Size:    {0}", mail.GetStringSize() );
                }
            }
            else
            {
                Console.WriteLine( "Error" );
            }

            Console.WriteLine( "" );
            Console.WriteLine( "Please press any key" );
            Console.ReadKey();
        }
    }
}
