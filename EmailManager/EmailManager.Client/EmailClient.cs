using MailKit.Net.Imap;
using MailKit;
using MimeKit;
using System;

namespace EmailManager.Client
{  

    public class EmailClient
    {
        public void FetchEmails(string user, string password)
        {
            using (var client = new ImapClient())
            {
                // Conectar al servidor IMAP de Gmail
                client.Connect("imap.gmail.com", 993, true);

                // Autenticarse con credenciales de usuario
                client.Authenticate(user, password);

                // Seleccionar la bandeja de entrada
                var inbox = client.Inbox;
                inbox.Open(FolderAccess.ReadOnly);

                Console.WriteLine($"Número de mensajes: {inbox.Count}");

                // Leer los últimos 10 mensajes
                for (int i = Math.Max(0, inbox.Count - 10); i < inbox.Count; i++)
                {
                    var message = inbox.GetMessage(i);
                    Console.WriteLine($"Asunto: {message.Subject}");
                }

                client.Disconnect(true);
            }
        }
    }
}
