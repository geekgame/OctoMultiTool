//
// csc wsserver.cs
// wsserver.exe

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

public class WebSocketClient
{
    public static List<WebSocketClient> clients = new List<WebSocketClient>();

    public TcpClient client;
    public NetworkStream stream;

    public WebSocketClient(TcpClient client)
    {
        clients.Add(this);
        this.client = client;
        stream = client.GetStream();

        // enter to an infinite in another thread to be able to handle every change in stream
        new Thread(ClientListenerThread).Start();
    }

    public void Send(string message)
    {
        // Message to send
        string text_to_answer = message;
        int msglen = text_to_answer.Length;

        // Send back the same message
        byte[] response = new byte[100 + msglen];

        // Set FIN to true, opcode to 1 (text message)
        response[0] = 0b10000001;

        // Set mask bit to false
        response[1] &= 0b01111111;

        int starts_writing_payload_at = 0;

        // Set payload length to msglen
        if (msglen < 126)
        {
            response[1] |= (byte)msglen;
            starts_writing_payload_at = 2;
        }
        else if (msglen < 65536)
        {
            response[1] |= 126;
            byte[] len = BitConverter.GetBytes((ushort)msglen);
            response[2] = len[1];
            response[3] = len[0];
            starts_writing_payload_at = 4;
        }
        else
        {
            response[1] |= 127;
            byte[] len = BitConverter.GetBytes(msglen);
            response[2] = len[7];
            response[3] = len[6];
            response[4] = len[5];
            response[5] = len[4];
            response[6] = len[3];
            response[7] = len[2];
            response[8] = len[1];
            response[9] = len[0];
            starts_writing_payload_at = 10;
        }

        // Set payload to the same message
        for (ulong i = 0; i < (ulong)msglen; ++i)
            response[starts_writing_payload_at + (int)i] = Encoding.UTF8.GetBytes(text_to_answer)[(int)i];

        client.GetStream().Write(response, 0, response.Length);
    }

    public void ClientListenerThread()
    {
        while (true)
        {
            while (!stream.DataAvailable) ;
            while (client.Available < 3) ; // match against "get"

            byte[] bytes = new byte[client.Available];
            stream.Read(bytes, 0, client.Available);
            string s = Encoding.UTF8.GetString(bytes);

            if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
            {
                Console.WriteLine("=====Handshaking from client=====\n{0}", s);

                // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                // 3. Compute SHA-1 and Base64 hash of the new value
                // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                byte[] response = Encoding.UTF8.GetBytes(
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                stream.Write(response, 0, response.Length);
            }
            else
            {
                bool fin = (bytes[0] & 0b10000000) != 0,
                    mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"
                int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                    offset = 2;
                ulong msglen = (ulong)(bytes[1] & 0b01111111);

                string text = "";

                if (msglen == 126)
                {
                    // bytes are reversed because websocket will print them in Big-Endian, whereas
                    // BitConverter will want them arranged in little-endian on windows
                    msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                    offset = 4;
                }
                else if (msglen == 127)
                {
                    // To test the below code, we need to manually buffer larger messages — since the NIC's autobuffering
                    // may be too latency-friendly for this code to run (that is, we may have only some of the bytes in this
                    // websocket frame available through client.Available).
                    msglen = BitConverter.ToUInt64(new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] }, 0);
                    offset = 10;
                }

                if (msglen == 0)
                {
                    Console.WriteLine("msglen == 0");
                }
                else if (mask)
                {
                    byte[] decoded = new byte[msglen];
                    byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                    offset += 4;

                    for (ulong i = 0; i < msglen; ++i)
                        decoded[i] = (byte)(bytes[offset + (int)i] ^ masks[i % 4]);

                    text = Encoding.UTF8.GetString(decoded);
                    Console.WriteLine("{0}", text);
                }
                else
                    Console.WriteLine("mask bit not set");
            }

        }

    }


}

class WebsocketServer
{
    TcpListener server;

    public WebsocketServer(string ip, int port)
    {
        server = new TcpListener(IPAddress.Parse(ip), port);
        server.Start();
        Console.WriteLine("Server has started on {0}:{1}, Waiting for a connection…", ip, port);

        // Async accept client
        server.BeginAcceptTcpClient(new AsyncCallback(AcceptClient), server);
    }

    void AcceptClient(IAsyncResult ar)
    {
        TcpListener listener = (TcpListener)ar.AsyncState;
        TcpClient client = listener.EndAcceptTcpClient(ar);

        Console.WriteLine("A client connected: {0}", client.Client.RemoteEndPoint);

        // Start new thread to handle client
        new WebSocketClient(client);
    }

}