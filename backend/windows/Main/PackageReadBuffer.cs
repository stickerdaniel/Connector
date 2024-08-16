namespace Connector
{


       public class PackageReadBuffer
        {
            public long transmissionStartTime = 0;
            public byte[] data = new byte[1024];
            public int offset = 0;
        }
    
}