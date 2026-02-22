/*
 * Universal Data Sink project.
 * 
 * This is nothing but a UDP packet logger with some timestamping.
 * 
 * The only 'added' function is to allow the creation of files programmatically.
 * 
 * So, on the control UDP port 3430, the user needs to specify the absolute path of the file to be written to.
 * 
 * The code creates this file (or appends to it if it already exists), and then it writes whatever data comes in,
 * literally as they come in trhough the data UDP port 3431, to the file specified.
 * 
 * The file being written to ---tries-- to maintain a CSV-type format. The first column is UTC unix time in milliseconds.
 * Then, there is a comma, and whatever came in the data packet. After the packet, a new line character is inserted.
 * 
 * <unixTimeNowInMs>,<raw packet data without any scrutiny or sanity check applied to><\n>
 * 
 * How you use this, and what format you use internally, is entirely up to you.
 * 
 */

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class UniversalDataSink
{

    // Public variables.
    public static UInt16 udpControlPort = 3430;
    public static UInt16 udpDataPort = 3431;


    // The file path.
    public static string filePath; // According to this, we are safe until we have more than 2^32 characters in the path.


    // Network interfaces

    static UdpClient controlInterface = new UdpClient(udpControlPort);
    static UdpClient dataInterface = new UdpClient(udpDataPort);



    static void Main()
    {

        // Set US English as default. For the number formatting to be consistent.
        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");


        Console.WriteLine("Listening on localhost:{0} for the file name, and localhost:{1} for data.", udpControlPort, udpDataPort);
        
        // Define IP endpoint for the control packet

        IPEndPoint controlIpEndPoint = new IPEndPoint(IPAddress.Any, udpControlPort);


        // start the async listener for data.
        dataInterface.BeginReceive(new AsyncCallback(receive_data), dataInterface);

        filePath = "if_you_see_this_file_then_configure_your_path_and_then_send_data.csv"; // Fallback mode.

        string oldFilePath = filePath; // We use this to detect if the file path has changed.



        // Stay here forever, unless we press escape
        while( true )
        {
            // Check if there is any control packet coming in. Hang here until there is something here.
            Byte[] controlPacketPayload = controlInterface.Receive(ref controlIpEndPoint);


            filePath = Encoding.ASCII.GetString(controlPacketPayload);

            if(!is_the_file_path_valid(filePath))
            {
                Console.WriteLine("The file path received seems invalid. See:");
                Console.WriteLine(filePath);
                // There is nothing else we can do, let's wait for the next control packet.
                break;
            }

            // Has the file path changed?
            if(!String.Equals(filePath, oldFilePath))
            {
                // Update oldFilePath, so this would trigger only once per update.
                oldFilePath = filePath;

                // The file path has changed, let the user know.
                Console.Clear();
                Console.WriteLine("Control port: {0}, data port {1}", udpControlPort, udpDataPort);
                Console.WriteLine("At timestamp {0}, new data file path received:", unix_time_now_in_ms());
                Console.WriteLine(filePath);
                Console.WriteLine("From this point onwards, every data sent to UDP Port {0} will be written into this file.", udpDataPort);
                Console.WriteLine("Press Ctrl+C in this window to terminate.");
            }

            // From this point onwards, everything is asynchronous.

        }
           
        

        
    }




    // This function gets executed when there is a packet in the buffer.
    // Original code: https://yal.cc/cs-dotnet-asynchronous-udp-example/
    static void receive_data(IAsyncResult result)
    {
        UdpClient socket = result.AsyncState as UdpClient; // set the client to be asynchronous maybe?

        IPEndPoint dataIpEndPoint = new IPEndPoint(IPAddress.Any, udpDataPort); // Accept packets only from the server.

        

        Byte[] packetPayload = socket.EndReceive(result, ref dataIpEndPoint);

        /*
         * In order to minimise possible data loss, the file in filePath
         * is being opneed to be appended to, then wriiten into, then closed.
         * 
         * Not sure how the OS will tolerate this type of hammering.
         * 
         * There is some exception management here too, just in case.
         */


        try
        {
            FileStream theDataFile = File.Open(filePath, FileMode.Append);

            // Write the current timestamp into the file.
            string timeStampAndComma = unix_time_now_in_ms() + ",";
            Byte[] timeStampAnDCommaAsByteArray = Encoding.ASCII.GetBytes(timeStampAndComma);
            // String writing is easy.
            theDataFile.Write(timeStampAnDCommaAsByteArray, 0, timeStampAnDCommaAsByteArray.Length);


            // Write the contents of the buffer.
            theDataFile.Write(packetPayload, 0, packetPayload.Length);

            // Add a newline at the end
            Byte[] newLineInthisMachine = Encoding.ASCII.GetBytes("\n"); // This could be CR+LF, or LF, depedning on the system.
            theDataFile.Write(newLineInthisMachine, 0, newLineInthisMachine.Length);

            // Close the file
            theDataFile.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());

            // If the file could not be opened for some reason, the packet contents are discarded.
            socket.BeginReceive(new AsyncCallback(receive_data), socket);
            return;
        }

    

      
        // Once the transfer is done, restart the receive process again.
        socket.BeginReceive(new AsyncCallback(receive_data), socket);

    }





    // Tell the time. Exactly.
    private static string unix_time_now_in_ms()
    {
        // Get the UTC time now
        DateTimeOffset time_now = DateTimeOffset.UtcNow;
        // Convert it to milliseconds
        long the_time = time_now.ToUnixTimeMilliseconds();
        // Format it into a string
        return the_time.ToString();
    }


    // Some AI generated code to validate the file path.
    public static bool is_the_file_path_valid(string path)
    {
        // Check for invalid characters
        if (string.IsNullOrWhiteSpace(path) || path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return false;
        }

        try
        {
            // Attempt to create a FileInfo object to validate the path format
            var fileInfo = new FileInfo(path);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

}


