using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System;
using System.Collections;
using System.Text;
using System.Threading;

namespace Utils.Tools
{
    #region CONVERTER
    public static class Convert
    {
        #region ENUM
        public enum Endian
        {
            BigEndian,
            LittleEndian
        }
        #endregion

        #region TO INT/UINT/SHORT/USHORT
        /// <summary>
        /// Converts a hex string to a 32-bit signed integer.
        /// </summary>
        /// <remarks>
        /// For unsigned hex values use the 'HexToUInt' method to prevent unexpected negatives.
        /// 0x7FFFFFFF converts to  2147483647
        /// 0x80000000 converts to -2147483648
        /// </remarks>
        /// <param name="Input">The hex string to convert.</param>
        /// <returns>The signed hex value as an int.</returns>
        public static int ToInt(string Input)
        {
            return System.Convert.ToInt32(Input, 16);
        }

        /// <summary>
        /// Converts a byte[] to a 32-bit signed integer.
        /// </summary>
        /// <param name="Input">the byte[] to convert. Must be no more than 4 bytes.</param>
        /// <param name="Endianness">Whether top perform a big endian or little endian conversion.</param>
        /// <returns>The value of the byte[] as an int.</returns>
        public static int ToInt(byte[] Input, Endian Endianness)
        {
            if (Input.Length > 4)
            { throw new ArgumentOutOfRangeException("Input", "Input is too large to convert to int."); }

            int Output = 0;
            for (int i = 0; i < Input.Length; i++)
            {
                switch (Endianness)
                {
                    case Endian.BigEndian:
                        Output += Input[i] << (8 * (Input.Length - (i + 1))); //24 << 16 << 8 << 0
                        break;
                    case Endian.LittleEndian:
                        Output += Input[i] << (8 * i); //0 << 8 << 16 << 24
                        break;
                }
            }
            return Output;
        }

        /// <summary>
        /// Converts a byte[] to a 32-bit signed short.
        /// </summary>
        /// <param name="Endianness">Whether top perform a big endian or little endian conversion.</param>
        /// <param name="Input">The byte[] to convert. Must be no more than 4 bytes.</param>
        /// <returns>The value of the byte[] as a short.</returns>
        public static short ToShort(Endian Endianness, params byte[] Input)
        {
            // Won't fit in an int.
            if (Input.Length > 2)
            { throw new ArgumentOutOfRangeException("Input", "Input is too large to convert to short"); }

            // Initialize the output value.
            short Output = (short)(ToInt(Input, Endianness));
            return Output;
        }

        /// <summary>
        /// Converts a hex string to a 32-bit unsigned integer.
        /// </summary>
        /// <param name="Input">The hex string to convert.</param>
        /// <returns>The unsigned hex value as a uint.</returns>
        public static uint ToUInt(string Input)
        {
            return (uint)System.Convert.ToInt32(Input, 16);
        }
        /// <summary>
        /// Converts a byte[] to a 32-bit unsigned integer.
        /// </summary>
        /// <param name="Input">the byte[] to convert. Must be no more than 4 bytes.</param>
        /// <param name="Endianness">Whether top perform a big endian or little endian conversion.</param>
        /// <returns>The value of the byte[] as a uint.</returns>
        public static uint ToUInt(byte[] Input, Endian Endianness)
        {
            if (Input.Length > 4)
            { throw new ArgumentOutOfRangeException("Input", "Input is too large to convert to uint."); }

            uint Output = (uint)ToInt(Input, Endianness);
            return Output;
        }

        /// <summary>
        /// Converts a byte[] to a 32-bit unsigned short.
        /// </summary>
        /// <param name="Endianness">Whether top perform a big endian or little endian conversion.</param>
        /// <param name="Input">The byte[] to convert. Must be no more than 4 bytes.</param>
        /// <returns>The value of the byte[] as a ushort.</returns>
        public static ushort ToUShort(Endian Endianness, params byte[] Input)
        {
            // Won't fit in a ushort.
            if (Input.Length > 2)
            { throw new ArgumentOutOfRangeException("Input", "Input is too large to convert to ushort."); }

            ushort Output = (ushort)(ToInt(Input, Endianness));
            return Output;
        }
        #endregion

        #region TO BYTES
        /// <summary>
        /// Converts and int to byte[].
        /// </summary>
        /// <param name="Input">The int to covert.</param>
        /// <param name="Endianness">Whether top perform a big endian or little endian conversion.</param>
        /// <returns>The converted byte[].</returns>
        public static byte[] ToBytes(int Input, Endian Endianness)
        {
            byte[] Output = new byte[4];
            for (int i = 0; i < Output.Length; i++)
            {
                switch (Endianness)
                {
                    case Endian.BigEndian:
                        Output[i] = (byte)(Input >> (8 * (Output.Length - (i + 1))) & 0xFF); //24 >> 16 >> 8 >> 0
                        break;
                    case Endian.LittleEndian:
                        Output[i] = (byte)(Input >> (8 * i) & 0xFF); //0 >> 8 >> 16 >> 24
                        break;
                }
            }
            return Output;
        }

        private static readonly string[] AllowedNonHex = { " ", ":", "-", "0x", "#", "h", "$", "x" };
        /// <summary>
        /// Converts a hex value to its equivalent byte[]
        /// </summary>
        /// <param name="input">Hex string conataining the value to represent as a byte[]</param>
        /// <returns>byte[]</returns>
        public static byte[] ToBytes(string Input, bool Hex)
        {
            byte[] b = new byte[0];
            if (Hex)
            {
                StringBuilder sb = new StringBuilder(Input.ToUpper());
                foreach (string s in AllowedNonHex)
                { sb.Replace(s.ToUpper(), ""); }
                if (sb.Length == 0)
                { throw new ArgumentException("Argument 'input' is not a valid hex string."); }

                int byteLength = (sb.Length % 2) == 1 ? (sb.Length + 1) / 2 : sb.Length / 2;
                b = new byte[byteLength];
                char c;

                for (int i = sb.Length - 1; i > -1; i -= 2)
                {
                    c = (char)(sb[i]);
                    if ((c >= 0x30 && c <= 0x39) || (c >= 0x41 && c <= 0x46))
                    { b[i / 2] = (byte)(c < 0x3A ? c - 0x30 : c - 0x37); }
                    else
                    { throw new ArgumentException("Not a hex string."); }
                    if (i > 0)
                    {
                        c = (char)sb[i - 1];
                        if ((c >= 0x30 && c <= 0x39) || (c >= 0x41 && c <= 0x46))
                        { b[i / 2] += (byte)((c < 0x3A ? c - 0x30 : c - 0x37) << 4); }
                        else
                        { throw new ArgumentException("Not a hex string."); }
                    }
                }
            }
            else
            { }
            return b;
        }
        #endregion

        #region TO HEX STRING
        /****************************************************************
         BitConverter
         ****************************************************************/
        /// <summary>
        /// Converts a byte array to a hex string
        /// </summary>
        /// <param name="value">The byte array</param>
        /// <param name="index">The array index to start from. Default 0</param>
        /// <returns>Hex string</returns>
        /// <remarks>Taken from http://forums.netduino.com/index.php?/topic/735-convert-a-byte-to-hex/#entry13529 </remarks>
        public static string ToHex(byte[] value, int index = 0)
        {
            return ToHex(value, index, value.Length - index);
        }
        /// <summary>
        /// Converts a byte array to a hex string
        /// </summary>
        /// <param name="value">The byte array</param>
        /// <param name="index">The array index to start from</param>
        /// <param name="length">The number of bytes to convert</param>
        /// <returns>Hex string</returns>
        /// <remarks>Taken from http://forums.netduino.com/index.php?/topic/735-convert-a-byte-to-hex/#entry13529 </remarks>
        public static string ToHex(byte[] value, int index, int length)
        {
            char[] c = new char[length * 3];
            byte b;

            for (int y = 0, x = 0; y < length; ++y, ++x)
            {
                b = (byte)(value[index + y] >> 4);
                c[x] = (char)(b > 9 ? b + 0x37 : b + 0x30);
                b = (byte)(value[index + y] & 0xF);
                c[++x] = (char)(b > 9 ? b + 0x37 : b + 0x30);
                c[++x] = ' ';
            }
            return new string(c, 0, c.Length - 1);
        }

        /// <summary>
        /// Converts an ASCII string to the equivalent hex string.
        /// </summary>
        /// <param name="Input">The ASCII string to convert.</param>
        /// <returns>A string of hex values.</returns>
        public static string ToHex(string Input)
        {
            StringBuilder Output = new StringBuilder();
            foreach (char c in Input)
            {
                Output.Append(ToHex(new byte[] { (byte)c }));
            }
            return Output.ToString().Trim();
        }
        #endregion

        #region TO ASCII CHAR/STRING
        public static char ToChar(byte Input)
        {
            return System.Convert.ToChar(Input);
        }

        /// <summary>
        /// Converts a hex string to the equivalent ASCII string.
        /// </summary>
        /// <param name="Input">The hex string to convert.</param>
        /// <returns>An ASCII string.</returns>
        public static string ToString(string Input)
        {
            return new string(Encoding.UTF8.GetChars(ToBytes(Input, true)));
        }

        /// <summary>
        /// Converts a byte[] to the equivalent ASCII string.
        /// </summary>
        /// <param name="Input">The byte[] to convert.</param>
        /// <returns>An ASCII string.</returns>
        public static string ToString(params byte[] Input)
        {
            return new string(Encoding.UTF8.GetChars(Input));
        }
        #endregion
    }
    #endregion

    #region MATH
    public static class Math
    {
        #region AVERAGE
        /// <summary>
        /// Averages a collection of numbers.
        /// </summary>
        /// <param name="Values">The collection to averge.</param>
        /// <returns>The averaged value.</returns>
        public static double Avg(IList Values)
        {
            return Avg(Values, 0, Values.Count);
        }

        /// <summary>
        /// Averages a collection of numbers.
        /// </summary>
        /// <param name="Values">The collection to averge.</param>
        /// <param name="Offset">The position in the collection to start processing from.</param>
        /// <param name="Count">How many values in the collection to process.</param>
        /// <returns>The averaged value.</returns>
        public static double Avg(IList Values, int Offset, int Count)
        {
            // Don't bother with null or empty collections.
            if (Values == null || Values.Count == 0)
            { throw new ArgumentNullException(); }

            // Make sure count isn't larger than the collection size.
            if (Values.Count < Count)
            { Count = Values.Count; }

            // Keep a sum of all the values so we can average them.
            double sum = 0;

            for (int i = Offset; i < Count + Offset; i++)
            {
                double val = 0;
                if (Values[i] is double) // If the value is a double we can simply cast it.
                {
                    val = (double)Values[i];
                }
                else if (Values[i] is int) // If the value is an int we need to parse it.
                {
                    val = double.Parse(Values[i].ToString());
                }
                else // There is a non-number in the collection when there shouldn't be.
                { throw new ArgumentException("Value was not of type double or int"); }

                // Add the current value to the sum.
                sum += val;
            }
            // Average the sum.
            double avg = sum / Count;
            return avg;
        }
        #endregion

        #region MAXIMUM
        /// <summary>
        /// Gets the maximum value from a collection of numbers.
        /// </summary>
        /// <param name="Values">The collection to retreive the max value from.</param>
        /// <returns>The maximum value.</returns>
        public static double Max(IList Values)
        {
            return Max(Values, 0, Values.Count);
        }

        /// <summary>
        /// Gets the maximum value from a collection of numbers.
        /// </summary>
        /// <param name="Values">The collection to retreive the max value from.</param>
        /// <param name="Offset">The position in the collection to start processing from.</param>
        /// <param name="Count">How many values in the collection to process.</param>
        /// <returns>The maximum value.</returns>
        public static double Max(IList Values, int Offset, int Count)
        {
            // Don't bother with null or empty collections.
            if (Values == null || Values.Count == 0)
            { throw new ArgumentNullException(); }

            // Make sure count isn't larger than the collection size.
            if (Values.Count < Count)
            { Count = Values.Count; }

            // Create a double to store the current maximum.
            double max = double.MinValue; // Assign to double.MinValue so it is <= the first value
            for (int i = Offset; i < Count + Offset; i++)
            {
                if (Values[i] is double) // If the value is a double we can simply cast it.
                {
                    if ((double)Values[i] > max)
                    {
                        max = (double)Values[i];
                    }
                }
                else if (Values[i] is int) // If the value is an int we need to parse it.
                {
                    if (double.Parse(Values[i].ToString()) > max)
                    {
                        max = double.Parse(Values[i].ToString());
                    }
                }
                else // There is a non-number in the collection when there shouldn't be.
                { throw new ArgumentException("Value was not of type double or int"); }
            }

            return max;
        }
        #endregion

        #region CHECKSUM
        /// <summary>
        /// Calculate the sum of each byte in the specified array.
        /// </summary>
        /// <param name="Data">The byte[] to sum.</param>
        /// <returns>The sum of the byte[].</returns>
        public static int CalculateChecksum(byte[] Data)
        {
            int checksum = 0;
            for (int i = 0; i < Data.Length; i++)
            {
                checksum += Data[i];
            }
            return checksum;
        }

        /// <summary>
        /// Verifies whether the checksum matches the actual data.
        /// </summary>
        /// <param name="RawData">The raw byte[] to verify using the chescksum. 
        /// Returns with the checksum bytes removed.</param>
        /// <returns>Whether or not the checksums match.</returns>
        public static bool VerifyChecksum(ref byte[] RawData)
        {
            byte[] ProcessedData = new byte[RawData.Length - 2];

            for (int i = 0; i < ProcessedData.Length; i++)
            {
                ProcessedData[i] = RawData[i];
            }

            int RawChecksum = Convert.ToInt(
                new byte[] { RawData[RawData.Length - 2], RawData[RawData.Length - 1] },
                Convert.Endian.BigEndian);
            int ProcessedChecksum = CalculateChecksum(ProcessedData);

            if (RawChecksum == ProcessedChecksum)
            { return true; }
            else
            { return false; }
        }
        #endregion
    }
    #endregion

    #region PARSER
    public static class Parse
    {
        #region HEX STRING
        /// <summary>
        /// Removes all non-hex characters from a string.
        /// </summary>
        /// <param name="Input">String to parse</param>
        /// <returns>A string containing an even number of only hex characters (0-F) padded with prepending 0 if neccesssary.</returns>
        public static string HexString(string Input)
        {
            string returnHex;

            // Create a string builder to put together the hex string (upper case).
            StringBuilder sb = new StringBuilder(Input.ToUpper());

            ArrayList hex = new ArrayList();
            char c;
            // Remove all characters that aren't 0-9 or A-F.
            for (int i = sb.Length - 1; i > -1; i -= 2)
            {
                c = (char)(sb[i]);
                if ((c >= 0x30 && c <= 0x39) || (c >= 0x41 && c <= 0x46))
                { hex.Insert(0, c); }
                if (i > 0)
                {
                    c = (char)sb[i - 1];
                    if ((c >= 0x30 && c <= 0x39) || (c >= 0x41 && c <= 0x46))
                    { hex.Insert(0, c); }
                }
            }
            if (sb.Length == 0)
            { throw new ArgumentException("Argument 'input' is not a valid hex string."); }
            else
            {
                // Pad 0s if necessary.
                if (hex.Count % 2 != 0)
                { hex.Insert(0, '0'); }

                returnHex = new string((char[])hex.ToArray(typeof(char)));
            }
            return returnHex;
        }
        #endregion
    }
    #endregion

    #region LEDS
    public static class Lights
    {
        #region PRIVATE VARIABLES
        private static Thread flasher;
        private static LEDs leds = new LEDs();
        #endregion

        #region LED COLLECTION
        public class LEDs : IEnumerable
        {
            public class LED
            {
                // The digital GPIO port the LED is connected to.
                private OutputPort _port;
                // Counter used to determine when to change states.
                private int opCount = 0;

                public Pattern FlashPattern { get; set; }

                /// <summary>
                /// Gets or sets the current state of the LED.
                /// </summary>
                public bool CurrentState
                {
                    get
                    { return _port.Read(); }
                    set
                    {
                        ResetOpCount();
                        _port.Write(value);
                    }
                }

                public LED(Cpu.Pin port, Pattern pattern)
                {
                    FlashPattern = pattern;
                    _port = new OutputPort(port, false);
                }

                /// <summary>
                /// Resets the operations count to 0. Do this every state change.
                /// </summary>
                public void ResetOpCount()
                {
                    opCount = 0;
                }

                /// <summary>
                /// Updates the state of the LED.
                /// </summary>
                public void Update()
                {
                    switch (FlashPattern)
                    {
                        case Pattern.ShortSteady:
                            if (opCount > 5)
                            { CurrentState = !CurrentState; }
                            break;
                        case Pattern.LongSteady:
                            if (opCount > 20)
                            { CurrentState = !CurrentState; }
                            break;
                        case Pattern.ShortOn:
                            if (opCount > 1 && CurrentState == true)
                            { CurrentState = false; }
                            else if (opCount > 50 && CurrentState == false)
                            { CurrentState = true; }
                            break;
                        case Pattern.LongOn:
                            if (opCount > 50 && CurrentState == true)
                            { CurrentState = false; }
                            else if (opCount > 1 && CurrentState == false)
                            { CurrentState = true; }
                            break;
                        case Pattern.On:
                            CurrentState = true;
                            break;
                        case Pattern.Off:
                            CurrentState = false;
                            break;
                        default:
                            break;
                    }

                    ++opCount;
                }
            }

            LED[] leds = new LED[] 
            {
                // The Red LED
                new LED(Pins.GPIO_PIN_D10, Pattern.Off),
                // Orange LED
                new LED(Pins.GPIO_PIN_D11, Pattern.Off),
                // Yellow LED
                new LED(Pins.GPIO_PIN_D12, Pattern.Off),
                // Green LED
                new LED(Pins.GPIO_PIN_D13, Pattern.Off),
                // Blue Onboard LED
                new LED(Pins.ONBOARD_LED, Pattern.Off)
            };

            public IEnumerator GetEnumerator()
            {
                return leds.GetEnumerator();
            }

            public LED this[Light led]
            {
                get
                { return leds[(int)led]; }
            }
        }
        #endregion

        #region ENUMs
        public enum Light
        {
            /// <summary>The Red LED</summary>
            Red,
            /// <summary>The Orange LED</summary>
            Orange,
            /// <summary>The Yellow LED</summary>
            Yellow,
            /// <summary>The Green LED</summary>
            Green,
            /// <summary>The Blue Onboard LED</summary>
            Onboard,
        }
        public enum Pattern
        {
            /// <summary>LED is permanently off</summary>
            Off,
            /// <summary>LED is permanently on</summary>
            On,
            /// <summary>250ms steady on/off cycle</summary>
            ShortSteady,
            /// <summary>1000ms steady on off cycle</summary>
            LongSteady,
            /// <summary>50ms on/2500ms off cycle</summary>
            ShortOn,
            /// <summary>2500 on/50ms off cycle</summary>
            LongOn,
            /// <summary>Manually controlled</summary>
            Manual
        }
        #endregion

        #region INITIALIZATION
        public static void Load()
        {
            // Start the flashing thread.
            flasher = new Thread(Update);
            flasher.Start();
        }
        #endregion

        #region RUN THREAD
        /// <summary>
        /// Updates the LED states.
        /// </summary>
        private static void Update()
        {
            while (true)
            {
                // Update the LED states.
                foreach (LEDs.LED led in leds)
                {
                    led.Update();
                }
                Thread.Sleep(50);
            }
        }
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Switches the state of an LED.
        /// </summary>
        /// <param name="led">The LED to switch.</param>
        public static void SwitchLight(Light led)
        {
            leds[led].CurrentState = !leds[led].CurrentState;
        }

        /// <summary>
        /// Sets the state of an LED.
        /// </summary>
        /// <param name="led">The LED to set.</param>
        /// <param name="newState">The state to set the LED to. True for on, false for off.</param>
        public static void SetLightState(Light led, bool newState)
        {
            leds[led].CurrentState = newState;
        }

        /// <summary>
        /// Sets the flash pattern of an LED.
        /// </summary>
        /// <param name="led">The LED to set.</param>
        /// <param name="pattern">The pattern to assign.</param>
        public static void SetLightPattern(Light led, Pattern pattern)
        {
            leds[led].FlashPattern = pattern;
        }
        #endregion
    }
    #endregion

    #region EXCEPTIONS
    /// <summary>
    /// The exception that is thrown when a data stream ends suddenly without returning all expected data.
    /// </summary>
    public class UnexpectedEndOfStreamException : Exception
    {
        public UnexpectedEndOfStreamException() : base() { }
        public UnexpectedEndOfStreamException(string message) : base(message) { }
        public UnexpectedEndOfStreamException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// The exception that is thrown when an unexpected response is received or an expected response is not received.
    /// </summary>
    public class UnexpectedResponseException : Exception
    {
        public UnexpectedResponseException() : base() { }
        public UnexpectedResponseException(string message) : base(message) { }
        public UnexpectedResponseException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// The exception that is thrown when an unexpected response is received or an expected response is not received.
    /// </summary>
    public class BufferOverflowException : Exception
    {
        public BufferOverflowException() : base() { }
        public BufferOverflowException(string message) : base(message) { }
        public BufferOverflowException(string message, Exception inner) : base(message, inner) { }
    }
    #endregion

    #region TIMEOUT
    public class Timeout
    {
        private int timeoutCounter;
        /// <summary>The number of seconds remaining.</summary>
        public int TimeoutCounter
        {
            get
            { return timeoutCounter; }
            private set
            { timeoutCounter = value; }
        }
        private int timeoutDefault = 30;
        /// <summary>The timeout period in seconds to wait for certain commands. Intial value is 30.</summary>
        public int DefaultTimeout
        {
            get
            { return timeoutDefault; }
            set
            { timeoutDefault = value; }
        }

        public Timeout()
        {
            Set();
        }

        public Timeout(int defaultTimeout)
        {
            timeoutDefault = defaultTimeout;
            Set();
        }

        /// <summary>
        /// Sets the timeout counter to the default value.
        /// </summary>
        public void Set()
        {
            timeoutCounter = timeoutDefault;
        }

        /// <summary>
        /// Sets the timeout counter to a specified value without changing the default timeout.
        /// </summary>
        /// <param name="value">The value to set the timeout to.</param>
        public void Set(int value)
        {
            timeoutCounter = value;
        }

        public static Timeout operator ++(Timeout t)
        {
            Timeout r = new Timeout();
            r.timeoutCounter = t.timeoutCounter + 1;
            return r;
        }

        public static Timeout operator --(Timeout t)
        {
            Timeout r = new Timeout();
            r.timeoutCounter = t.timeoutCounter - 1;
            return r;
        }

        public static bool operator >(Timeout t, int i)
        {
            return t.timeoutCounter > i;
        }

        public static bool operator >(Timeout t1, Timeout t2)
        {
            return t1.timeoutCounter > t2.timeoutCounter;
        }

        public static bool operator <(Timeout t, int i)
        {
            return t.timeoutCounter < i;
        }

        public static bool operator <(Timeout t1, Timeout t2)
        {
            return t1.timeoutCounter < t2.timeoutCounter;
        }

        public static bool operator ==(Timeout t, int i)
        {
            return t.timeoutCounter == i;
        }

        public static bool operator ==(Timeout t1, Timeout t2)
        {
            return t1.timeoutCounter == t2.timeoutCounter;
        }

        public static bool operator !=(Timeout t, int i)
        {
            return t.timeoutCounter != i;
        }
        public static bool operator !=(Timeout t1, Timeout t2)
        {
            return t1.timeoutCounter != t2.timeoutCounter;
        }

        public static bool operator <=(Timeout t1, Timeout t2)
        {
            return t1.timeoutCounter <= t2.timeoutCounter;
        }

        public static bool operator >=(Timeout t1, Timeout t2)
        {
            return t1.timeoutCounter >= t2.timeoutCounter;
        }

        public override bool Equals(Object o)
        {
            // If parameter is null return false.
            if (o == null)
            {
                return false;
            }

            // If parameter cannot be cast to Timeout return false.
            Timeout t = o as Timeout;
            if ((System.Object)t == null)
            {
                return false;
            }

            // Return true if the fields match:
            return timeoutCounter == t.timeoutCounter;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    #endregion
}
