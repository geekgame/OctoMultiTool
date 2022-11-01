using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ConsoleApp1
{
    public class TcpSimpleServer
    {
        TcpListener server;

        List<TcpClient> clients = new List<TcpClient>();   

        // Add callback for when a client connects
        public delegate void ClientConnected(TcpClient client);
        public event ClientConnected OnClientConnected;

        // Add callback for when a client disconnects
        public delegate void ClientDisconnected(TcpClient client);
        public event ClientDisconnected OnClientDisconnected;

        // Add callback for when a client sends a message
        public delegate bool ClientMessage(TcpClient client, string message); // Return true if handled
        public event ClientMessage OnClientMessage;


        private TcpSimpleServer(int port)
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Logger.Log("Server has been started !");

            // Start listening for new clients
            server.BeginAcceptTcpClient(new AsyncCallback(AcceptClient), null);
        }

        private void AcceptClient(IAsyncResult ar)
        {
            TcpClient client = server.EndAcceptTcpClient(ar);
            OnClientConnected?.Invoke(client);

            // Logger.Log("Client connected !");
            clients.Add(client);

            // Start listening for their messages
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[client.ReceiveBufferSize];
            stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReadMessage), buffer);

            // Start listening for new clients
            server.BeginAcceptTcpClient(new AsyncCallback(AcceptClient), null);
        }

        private void ReadMessage(IAsyncResult ar)
        {

            byte[] buffer = (byte[])ar.AsyncState;
            NetworkStream stream = clients[0].GetStream();
            int bytesRead = stream.EndRead(ar);

            if (bytesRead > 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                // Logger.Log("From: " + clients[0].Client.RemoteEndPoint + " Message: " + message);

                // Start listening for their messages
                stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReadMessage), buffer);
            }
        }

        static TcpSimpleServer _inst;
        public static TcpSimpleServer Instance { get
            {
                if (_inst == null)
                    _inst = new TcpSimpleServer(8080);
                return _inst;
            } }

        public void Broadcast(string message)
        {

        }
    }
}
