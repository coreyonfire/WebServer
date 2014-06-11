using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServer
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    class MyWebServer
    {
        private TcpListener myListener;
        private int port = 7070;
        private String serverRoot = Directory.GetCurrentDirectory() + "\\";

        public MyWebServer()
        {
            try
            {
                // start listening on the port
                myListener = new TcpListener(Dns.GetHostEntry("localhost").AddressList[0], port);
                myListener.Start();
                Console.WriteLine("Web Server now listening on port " + port + ", press ^C to stop...");

                // start the thread which calls the startListen method
                Thread th = new Thread(new ThreadStart(startListen));
                th.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception has occured whilst listening: " + e.ToString());
            }
        }


        public string getDefaultFileName(string serverRoot, string localDir)
        {
            StreamReader sr;
            String line = "";

            try
            {
                // open the default.dat to find the list
                // of default files

                sr = new StreamReader(serverRoot + "data\\Default.dat");

                while ((line = sr.ReadLine()) != null)
                {
                    // look for default file in the root folder
                    if (File.Exists(localDir + line))
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An exception occured: " + e.ToString());
            }

            if (File.Exists(localDir + line))
                return line;
            else
                return "";
        }

        public string getLocalPath(string serverRoot, string dirName)
        {
            StreamReader sr;
            String line = "";
            String virtualDir = "";
            String realDir = "";
            int startPos = 0;

            // trim the dir name
            dirName.Trim();

            // convert root to lowercase
            serverRoot = serverRoot.ToLower();
            dirName = dirName.ToLower();

            try
            {
                // open the vdirs.dat file to find the list of directories!
                sr = new StreamReader("data\\Vdirs.dat");

                while ((line = sr.ReadLine()) != null)
                {
                    line.Trim();

                    if (line.Length > 0)
                    {
                        //find the separator
                        startPos = line.IndexOf(";");

                        // convert to lowercase
                        line = line.ToLower();

                        virtualDir = line.Substring(0, startPos);
                        realDir = serverRoot + line.Substring(startPos + 1);

                        if (dirName.Equals(virtualDir))
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An exception occured: " + e.ToString());
            }

            if (virtualDir.Equals(dirName))
                return realDir;
            else
                return "";
        }

        public string getMimeType(string serverRoot, string requestedFile)
        {
            StreamReader sr;
            String line = "";
            String mimeType = "";
            String fileExt = "";
            String mimeExt = "";

            // to lower case!
            requestedFile = requestedFile.ToLower();
            int startPos = requestedFile.IndexOf(".");
            fileExt = requestedFile.Substring(startPos);

            try
            {
                // get the list of virtual directories
                sr = new StreamReader(serverRoot + "data\\Mimes.dat");

                while ((line = sr.ReadLine()) != null)
                {
                    line.Trim();

                    if (line.Length > 0)
                    {
                        // find separator
                        startPos = line.IndexOf(";");

                        line = line.ToLower();

                        mimeExt = line.Substring(0, startPos);
                        mimeType = line.Substring(startPos + 1);

                        if (mimeExt.Equals(fileExt))
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An exception has occured: " + e.ToString());
            }
            if (fileExt.Equals(mimeExt))
                return mimeType;
            else
                return "";
        }

        public void sendHeader(string httpVersion, string mimeHeader, int totalBytes, string statusCode, ref Socket mySocket)
        {
            String buffer = "";

            // if mime type is not provided sest the default to text/html
            if (mimeHeader.Length == 0)
            {
                mimeHeader = "text/html";
            }

            buffer = buffer + httpVersion + statusCode + "\r\n";
            buffer = buffer + "Server: cx1193719-b\r\n"; // what does this do?
            buffer = buffer + "Content-Type: " + mimeHeader + "\r\n";
            buffer = buffer + "Accept-Ranges: bytes\r\n";
            buffer = buffer + "Content-Length: " + totalBytes + "\r\n\r\n";

            Byte[] sendData = Encoding.ASCII.GetBytes(buffer);

            sendToBrowser(sendData, ref mySocket);

            Console.WriteLine("Total Bytes: " + totalBytes);
        }

        public void sendToBrowser(string data, ref Socket mySocket) 
        {
            sendToBrowser(Encoding.ASCII.GetBytes(data), ref mySocket);
        }

        public void sendToBrowser(byte[] sendData, ref Socket mySocket)
        {
            int numBytes = 0;

            try
            {
                if (mySocket.Connected)
                {
                    if ((numBytes = mySocket.Send(sendData, sendData.Length, 0)) == -1)
                    {
                        Console.WriteLine("Socket Error: cannot send packet!");
                    }
                    else
                    {
                        Console.WriteLine("No. of bytes sent: {0}", numBytes);
                    }
                }
                else
                    Console.WriteLine("Connection Dropped.");
            }
            catch (Exception e)
            {
                Console.WriteLine("An exception has occured: " + e.ToString());
            }
        }

        public void startListen()
        {

            int startPos = 0;
            String request;
            String dirName;
            String requestedFile;
            String errorMessage;
            String localDir;
            String physicalFilePath = "";
            String formattedMessage = "";
            String response = "";

            
            while (true)
            {
                // Accept new connection
                Socket mySocket = myListener.AcceptSocket();

                Console.WriteLine("Socket Type: " + mySocket.SocketType);
                if (mySocket.Connected)
                {
                    Console.WriteLine("\nClient Connected!\n=================\nClient IP: {0}\n", mySocket.RemoteEndPoint);
                    mySocket.ReceiveTimeout = 4000;
                    mySocket.SendTimeout = 4000;

                    // Make a byte array and recieve the data
                    Byte[] receive = new Byte[1024];
                    try
                    {
                        int i = mySocket.Receive(receive, receive.Length, 0);
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        // handle it...gracefully, i guess
                        Console.WriteLine("TIMEOUT. " );
                        mySocket.Close();
                        continue;
                    }
                    // convert byte to string
                    string buffer = Encoding.ASCII.GetString(receive).Trim();

                    Console.WriteLine("Message received: \"{0}\"", buffer);
                    // Presently, I can only handle GET requests
                    if (!buffer.Substring(0, 3).Equals("GET"))
                    {
                        Console.WriteLine("Only GET is supported :(");
                        mySocket.Close();

                        continue;
                        //return;
                    }


                    // Look for HTTP request
                    startPos = buffer.IndexOf("HTTP", 1);

                    // get the HTTP text and version
                    string httpVersion = buffer.Substring(startPos, 8);

                    // ge tthe requested type and file/dir
                    request = buffer.Substring(0, startPos - 1);

                    // replace backslashes with forward ones
                    request.Replace("\\", "/");

                    // if the file name is not supplied add a forward slash to indicate it is a directory
                    // and that i will look for the default file name
                    if ((request.IndexOf(".") < 1) && (!request.EndsWith("/")))
                    {
                        request = request + "/";
                    }

                    // get the requested file name
                    startPos = request.LastIndexOf("/") + 1;
                    requestedFile = request.Substring(startPos);

                    // get the dir name
                    dirName = request.Substring(request.IndexOf("/"), request.LastIndexOf("/") - 3);

                    // -------------------------------
                    // Identify the physical directory
                    // -------------------------------

                    if (dirName.Equals("/"))
                    {
                        localDir = serverRoot;
                    }
                    else
                    {
                        // get the virtual dir
                        localDir = getLocalPath(serverRoot, dirName);

                        // make sure dir ends with slash
                        if (!localDir.EndsWith("\\"))
                        {
                            localDir += "\\";
                        }
                    }

                    Console.WriteLine("Directory requested: {0}", dirName);

                    // if the physical directory does not exist, display error message
                    if (localDir.Length == 0)
                    {
                        errorMessage = "<H2>Error! Requested directory does not exist!</H2><br>";

                        // format the message
                        sendHeader(httpVersion, "", errorMessage.Length, " 404 Not Found", ref mySocket);

                        // send to the browser
                        sendToBrowser(errorMessage, ref mySocket);

                        mySocket.Close();


                        continue;
                        //return;
                    }

                    // ------------------
                    // Identify file name
                    // ------------------

                    // if the file name is not supplied look in the default file list
                    if (requestedFile.Length == 0)
                    {
                        // get the default file name
                        requestedFile = getDefaultFileName(serverRoot, localDir);

                        if (requestedFile.Equals(""))
                        {
                            errorMessage = "<H2>Error!! No default file name specified!</H2><br>";
                            sendHeader(httpVersion, "", errorMessage.Length, " 404 Not Found", ref mySocket);
                            sendToBrowser(errorMessage, ref mySocket);
                            mySocket.Close();

                            continue;
                            //return;
                        }
                    }

                    // -------------
                    // Get MIME type
                    // -------------
                    String mimeType = getMimeType(serverRoot, requestedFile);
                    // physical path
                    physicalFilePath = localDir + requestedFile;
                    Console.WriteLine("File Requested: " + physicalFilePath);


                    if (File.Exists(physicalFilePath) == false)
                    {
                        errorMessage = "<H2>404 Error: File does not exist.</H2><br>";
                        sendHeader(httpVersion, "", errorMessage.Length, " 404 Not Found", ref mySocket);
                        sendToBrowser(errorMessage, ref mySocket);
                        Console.WriteLine(formattedMessage);
                    }

                    else
                    {
                        int totalBytes = 0;

                        response = "";

                        FileStream fs = new FileStream(physicalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                        // create a reader that can read bytes from the file stream

                        BinaryReader reader = new BinaryReader(fs);

                        byte[] bytes = new byte[fs.Length];

                        int read;

                        while ((read = reader.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            // read the file and write the data out to the network
                            response = response + Encoding.ASCII.GetString(bytes, 0, read);
                            totalBytes += read;
                        }
                        reader.Close();
                        fs.Close();

                        sendHeader(httpVersion, mimeType, totalBytes, " 200 OK", ref mySocket);
                        sendToBrowser(bytes, ref mySocket);

                    }
                    mySocket.Close();
                }
            }
        }


        static int Main(string[] args)
        {
            MyWebServer ws = new MyWebServer();
            return 0;
        }
    }

}
