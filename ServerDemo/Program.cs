using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Text;
using ServerFramework.NET;
namespace ServerDemo
{
    
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            /*
             * Create a server.
             * There are 4 constructors to choose from
             */
            Server server = new Server(port: 80, messageDelimiters: new List<char> { '\n', '\r' });
            server.OnMessageReceived += server_OnMessageReceived; 
            server.OnTooManyClients += server_OnTooManyClients;
            server.OnClientConnect += server_OnClientConnect;
            server.OnClientDisconnect += (o, i) => Console.WriteLine("Client disconnected");
            server.OnServerStop += (o, i) => 
            {
                Console.WriteLine("Server stopped");
                Application.Exit();
            };
            server.StartAsync();
            Application.Run();
        }
        /// <summary>
        /// Event handler for when a client connects
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="e">ClientEventArgs containing the Client</param>
        static void server_OnClientConnect(object sender, ClientEventArgs e)
        {
            Client client = e.Client; //Get the client
            /*
             * Example of adding objects to a Tag for identification and extensible functionality. 
             * What this does is add a string to identify the client to a List of objects (the list is not strictly necessary, but it allows room for expansion)
             * This can be expanded to include a class to periodically ping the client, or anything else
             */
            client.Tag = new List<object> { "test" }; 
        }

        /// <summary>
        /// Event handler for there are too many clients. This event is fired when a client tries to connect. Immediately afterwards, the connection is terminated.
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="e">ClientEventArgs containing the Client</param>
        static void server_OnTooManyClients(object sender, ClientEventArgs e)
        {
            Client client = e.Client;
            client.SendData("Sorry, too many clients!\n"); 
        }

        /// <summary>
        /// Event handler for when a client sends a message. Access the message with e.client.Message
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="e">ClientEventArgs containing the Client</param>
        static void server_OnMessageReceived(object sender, ClientEventArgs e)
        {

            Client client = e.Client; //Get the Client
            
            string msg = Encoding.ASCII.GetString(client.Message); //Get the message as a string
            string html = File.ReadAllText("index.html"); //Read the HTML file

            if (msg.Contains("GET")) //Is this a HTTP GET?
            {
                if (msg.Contains("favicon")) client.SendData("HTTP/1.1 404\r\n"); //There isn't a favicon for this site
                else //Send a HTTP response with the HTML data
                    client.SendData(
                        "HTTP/1.1 200 OK\r\nServer: test-b\r\nContent-Type: text/html\r\nAccept-Ranges: bytes\r\nContent-Length: " +
                        html.Length + "\r\n\r\n" + html);
            }
            else client.SendData("I heard you! " + Encoding.ASCII.GetString(client.Message) + "\n"); //If it is not a HTTP connection (e.g. telnet), handle it differently
        }
    }
}
