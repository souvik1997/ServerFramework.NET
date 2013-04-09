using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerFramework.NET
{
    public class Client
    {
        #region Instance variables
        private byte[] _message;
        private TcpClient _tcpClient;
        #endregion
        #region Properties
        /// <summary>
        /// Gets the underlying TcpClient
        /// </summary>
        public TcpClient TcpClient
        {
            get { return _tcpClient; }
            internal set { _tcpClient = value; }
        }

        /// <summary>
        /// Gets the message received
        /// </summary>
        public byte[] Message
        {
            get { return _message; }
            internal set { _message = value; }
        }

        /// <summary>
        /// Gets or sets an arbitrary object for identification and storing user state
        /// </summary>
        public object Tag { get; set; }
        #endregion
        #region Methods
        /// <summary>
        /// Sends data to the client and handles IOExceptions
        /// </summary>
        /// <param name="data">Array of bytes to send</param>
        public void SendData(byte[] data)
        {
            if (data == null) return;
            try
            {
                if (TcpClient.GetStream().CanWrite)
                    TcpClient.GetStream().Write(data, 0, data.Length);
            }
            catch (IOException)
            {
            }
        }
        /// <summary>
        /// Sends data to the client and handles IOExceptions
        /// </summary>
        /// <param name="data">String to send</param>
        public void SendData(string data)
        {
            SendData(GetBytes(data));
        }

        private static byte[] GetBytes(string str)
        {
            return str == null ? null : Encoding.ASCII.GetBytes(str);
        }
        #endregion
    }
    public delegate void ClientEventHandler(object sender, ClientEventArgs e);

    /// <summary>
    /// Used in events to send client information
    /// </summary>
    public class ClientEventArgs : EventArgs
    {
        public Client Client
        {   
            get;
            set;
        }
    }

    /// <summary>
    /// An asynchronous TCP server
    /// </summary>
    public class Server
    {
        #region Readonly instance variables
        private readonly int _dataSize = -1;
        private readonly int _maxNumOfClients = -1;
        private readonly int _port;
        private readonly List<Client> _clients;
        #endregion
        #region Instance variables
        private TcpListener _server;
        private List<char> _delimiters;
        #endregion
        #region Events
        /// <summary>
        /// This event is fired when a message is received. The Client and Message are guaranteed to be not null
        /// </summary>
        public event ClientEventHandler OnMessageReceived;
        /// <summary>
        /// This event is fired when a client tries to connect but the server has already reached the maximum number of clients
        /// </summary>
        public event ClientEventHandler OnTooManyClients;
        /// <summary>
        /// This event is fired when a client connects successfully
        /// </summary>
        public event ClientEventHandler OnClientConnect;
        /// <summary>
        /// This event is fired when a client connects successfully
        /// </summary>
        public event ClientEventHandler OnClientDisconnect;
        /// <summary>
        /// This event is fired when the server stops
        /// </summary>
        public event EventHandler OnServerStop;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor for Server
        /// </summary>
        /// <param name="port">Port on which to run server</param>
        /// <param name="dataSize">The size of each incoming message in bytes</param>
        /// <param name="maxNumOfClients">Maximum number of clients</param>
        public Server(int port, int dataSize, int maxNumOfClients) : this(port, dataSize)
        {
            _maxNumOfClients = maxNumOfClients;
        }
        /// <summary>
        /// Constructor for Server
        /// </summary>
        /// <param name="port">Port on which to run server</param>
        /// <param name="dataSize">The size of each incoming message in bytes</param>
        public Server(int port, int dataSize) : this(port)
        {
            _dataSize = dataSize;
        }
        /// <summary>
        /// Constructor for server
        /// </summary>
        /// <param name="port">Port on which to run server</param>
        /// <param name="messageDelimiters">List of characters to separate messages</param>
        /// <param name="maxNumOfClients">Maximum number of clients</param>
        public Server(int port, List<char> messageDelimiters, int maxNumOfClients) : this(port, messageDelimiters)
        {
            _maxNumOfClients = maxNumOfClients;
        }
        /// <summary>
        /// Constructor for server
        /// </summary>
        /// <param name="port">Port on which to run server</param>
        /// <param name="messageDelimiters">List of characters to separate messages</param>
        public Server(int port, List<char> messageDelimiters) : this(port)
        {
            _delimiters = messageDelimiters;
        }
        /// <summary>
        /// Constructor for server
        /// </summary>
        /// <param name="port">Port on which to run server</param>
        private Server(int port)
        {
            _port = port;
            _clients = new List<Client>();
        }
        #endregion
        #region Properties
        /// <summary>
        /// Gets a List of connected clients
        /// </summary>
        public List<Client> ConnectedClients
        {
            get { return _clients; }
        }

        /// <summary>
        /// Gets the maximum number of clients that can connect, or -1 if there is no limit
        /// </summary>
        public int MaximumClients
        {
            get { return _maxNumOfClients;  }
        }

        /// <summary>
        /// Gets the port on which the server is running
        /// </summary>
        public int Port
        {
            get { return _port; }
        }

        /// <summary>
        /// Gets or sets the list of accepted delimiters
        /// </summary>
        public List<char> Delimiters
        {
            get { return _delimiters; }
            set { _delimiters = value;  }
        }
        #endregion
        #region Client manipulation
        /// <summary>
        /// Cleanly closes a client connection
        /// </summary>
        /// <param name="client">The client whose connection should be closed</param>
        public void CloseConnection(Client client)
        {
            if (client == null) return;
            client.TcpClient.Close();
            _clients.Remove(client);
            FireClientDisconnectedEvent(client);
        }

        private void ClientAccepted(IAsyncResult iar)
        {
            if (_server == null) return;
            StartAcceptingClients();

            var listener = iar.AsyncState as TcpListener;
            if (listener == null)
                return;
            TcpClient tcpClient = listener.EndAcceptTcpClient(iar);

            var client = new Client {TcpClient = tcpClient};
            if (_maxNumOfClients >= 0 && _clients.Count >= _maxNumOfClients)
            {
                FireTooManyClientsEvent(client);
                client.TcpClient.Close();

                return;
            }
            _clients.Add(client);
            FireClientConnectedEvent(client);
            client.TcpClient.GetStream().BeginRead(new byte[0], 0, 0, ClientRead, client);
        }

        private void ClientRead(IAsyncResult iar) 
        {
            var listener = iar.AsyncState as Client;
            if (listener == null)
                return;
            try
            {
                listener.TcpClient.GetStream().EndRead(iar);
                var bytes = new List<byte>();
                if (_dataSize >= 0)
                    for (int x = 0, b = listener.TcpClient.GetStream().ReadByte(); x < _dataSize + 1; x++, b = listener.TcpClient.GetStream().ReadByte())
                    {
                        bytes.Add((byte) b);
                    }
                else
                    for (int x = 0, b = listener.TcpClient.GetStream().ReadByte(); !_delimiters.Contains((char)b); x++, b = listener.TcpClient.GetStream().ReadByte())
                    {
                        bytes.Add((byte) b);
                    }
                listener.Message = bytes.ToArray();

            }
            catch
            {
                CloseConnection(listener);
                listener = null;
            }
            finally
            {
                if (listener != null && listener.Message != null) FireMessageReceivedEvent(listener);

                if (listener != null && listener.TcpClient != null && listener.TcpClient.Connected && listener.TcpClient.GetStream().CanRead)
                {
                    listener.TcpClient.GetStream().BeginRead(new byte[0], 0, 0, ClientRead, listener);
                }
            }

        }

        private void StartAcceptingClients()
        {
            _server.BeginAcceptTcpClient(ClientAccepted, _server);
        }
        #endregion
        #region Event firing methods
        protected virtual void FireClientConnectedEvent(Client client)
        {
            if (OnClientConnect != null)
                OnClientConnect(this, new ClientEventArgs {Client = client});
        }
        protected virtual void FireClientDisconnectedEvent(Client client)
        {
            if (OnClientDisconnect != null)
                OnClientDisconnect(this, new ClientEventArgs { Client = client });
        }
        protected virtual void FireMessageReceivedEvent(Client client)
        {
            if (OnMessageReceived != null)
                OnMessageReceived(this, new ClientEventArgs { Client = client });
        }
        protected virtual void FireTooManyClientsEvent(Client client)
        {
            if (OnTooManyClients != null)
                OnTooManyClients(this, new ClientEventArgs { Client = client });
        }
        protected virtual void FireServerStopEvent()
        {
            if (OnServerStop != null)
                OnServerStop(this, EventArgs.Empty);
        }
        #endregion
        #region Start and stop
        /// <summary>
        /// Starts the server asynchronously
        /// </summary>
        public void StartAsync()
        {
            _server = new TcpListener(new IPEndPoint(IPAddress.Any, _port));
            try
            {
                _server.Start();
            }
            catch (SocketException)
            {
                return;
            }
            StartAcceptingClients();
        }

        /// <summary>
        /// Cleanly stops the server and closes all connections
        /// </summary>
        public void Stop()
        {
            try
            {
                foreach (Client client in _clients) client.TcpClient.Close();
                _server.Stop();
                _server = null;
            }
            catch (SocketException)
            {
            }
            finally
            {
                FireServerStopEvent();
            }
        }
        #endregion

        /// <summary>
        /// Overrides object.ToString(). 
        /// </summary>
        /// <returns>A string with server information</returns>
        public new string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Server running on port: " + _port);
            sb.AppendLine("Number of connected clients: " + ConnectedClients.Count);
            return sb.ToString();
        }
    }
}