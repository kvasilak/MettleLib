using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.IO.Ports;


namespace MettleLib
{
    public class MettleHead
    {
        //string RxString = string.Empty;

        public event TagHandeler TagEvents;
        public delegate void TagHandeler(TagEvent e);


        public event ErrorHandeler TagErrorEvent;
        public delegate void ErrorHandeler(string s);

        private List<Module> ModuleList = new List<Module>();
        private String RXBuffer = string.Empty; //new StringBuilder();
        private Module SelectedModule = null;

        private SerialPort TheSerialPort = new SerialPort();

        public void FindControlls(Form frmMain)
        {
            //Walk through each tab and find all our custom controls
            //then register them for the tag recieved event
            foreach (Control c in frmMain.Controls)
            {
                foreach (Control ctl in c.Controls)
                {


                    //determine if the control is one of our custom ones,
                    //our custom controls all implement ITagInterface
                    if (ctl is MettleLib.ITagInterface)
                    {
                        Trace.WriteLine("found, " + ctl.GetType().ToString());

                        TagEvents += new TagHandeler(((MettleLib.ITagInterface)ctl).UpdateEvent);
                        ((MettleLib.ITagInterface)ctl).Initialize();

                    }

                    if (ctl is MettleLib.ITagErrorInterface)
                    {
                        TagErrorEvent += new ErrorHandeler(((MettleLib.ITagErrorInterface)ctl).UpdateEvent);
                        ((MettleLib.ITagErrorInterface)ctl).Initialize();
                    }

                    //look for and register child controls in containers
                    //such as the panel and groupbox
                    foreach (Control child in ctl.Controls)
                    {
                        if (child is MettleLib.ITagInterface)
                        {
                            Trace.WriteLine("child, " + child.Name);

                            TagEvents += new TagHandeler(((MettleLib.ITagInterface)child).UpdateEvent);
                            ((MettleLib.ITagInterface)child).Initialize();
                        }
                    }
                }
            }
        }

        public void Close()
        { }

        public void Reset()
        { }

        public void Open()
        {
            if (!TheSerialPort.IsOpen)
            {
                try
                {
                    TheSerialPort.PortName = "COM3";  //Properties.Settings.Default.COMport;
                    TheSerialPort.BaudRate = 9600;  //int.Parse(Properties.Settings.Default.BaudRate.ToString());
                    TheSerialPort.DataBits = 8;
                    TheSerialPort.Parity = System.IO.Ports.Parity.None;
                    TheSerialPort.StopBits = System.IO.Ports.StopBits.One;
                    //serialPort1.RtsEnable = true;
                    TheSerialPort.Encoding = Encoding.GetEncoding(28591); //So I can read all 8 bits from the stupid serial port
                    TheSerialPort.Open();
                    TheSerialPort.DiscardInBuffer();

                    TheSerialPort.DataReceived += new SerialDataReceivedEventHandler(serialDataReceived);

                    //stripStatus.Text = "Running";
                    //stripError.Text = "No Errors";

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Serial port; " + ex.Message, "Error!");
                    //stripError.Text = "Serial open Error; " + ex.Message;
                }
            }
        }

        // close the serial port in a seperate thread to prevent
        //A GUI deadlock
        private void SafeSerialClose()
        {
            Thread myThread = new System.Threading.Thread(delegate()
            {
                if (TheSerialPort.IsOpen)
                {
                    TheSerialPort.Close();
                }
            });
            myThread.Start();
        }

        //we have recieved serial data, get a line of it and send it to the parser
        private void serialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string temp;
            string Rx;
            int position = 0;

            if (TheSerialPort.IsOpen)
            {
                int cr;
                try
                {
                    //get rx chars
                    RXBuffer += TheSerialPort.ReadExisting();

                    //is there a \n?
                    cr = RXBuffer.IndexOf("\n");

                    //there HAS to be at least 1 character to be at all valid;
                    while (cr >= 0)
                    {
                        //copy all data up to \n
                        //as long as there IS data
                        if (cr > 1)
                        {
                            Rx = RXBuffer.Substring(0, cr);

                            //Trace.WriteLine(Rx + "\n");

                            //Process the message
                            //Invoke(new EventHandler(HandleMesage));
                            //HandleMesage(Rx);

                            do //may have multiple tags per line
                            {
                                position = ParseTags(Rx, position);
                            }
                            while ((position > 0) && (position < Rx.Length));
                        }

                        //Copy everything after \n back into rx buffer, removing string just sent
                        int len = RXBuffer.Length - (cr + 1);
                        temp = string.Empty;

                        //Anything left to copy?
                        if (len > 0)
                        {
                            temp = RXBuffer.Substring(cr + 1, len);
                        }

                        RXBuffer = temp;

                        //any more \n?
                        cr = RXBuffer.IndexOf("\n");

                    }

                    //stripError.Text = "No Errors";
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("serial, " + ex.Message + "\n");
                    Type ep = ex.GetType();
                    return;
                }
            }
        }

        //find the next tag and data, set an event to all listeners
        private int ParseTags(string instr, int offset)
        {
            int start;
            int end = instr.Length;
            int comma;
            int comma2;
            int d;

            //Tag format is >string,string,string<
            start = instr.IndexOf(">", offset);

            if (start >= 0)//May be first character
            {
                end = instr.IndexOf("<", start + 1);

                if (end > 0)
                {
                    //found start and end, find the comma
                    comma = instr.IndexOf(",", start + 1);

                    if (comma > 0)
                    {
                        //find the second comma
                        comma2 = instr.IndexOf(",", comma + 1);

                        //find the second command AND need at last one char between second comma and <
                        if ( (comma2 > 0) && ((comma2 + 1) < end))
                        {
                            //set the tag recieved event
                            TagEvent t = new TagEvent();

                            //split the tag and cleanup any whitespace
                            t.ModuleName = instr.Substring(start + 1, comma - (start + 1)).Trim(); //module name
                            t.Name = instr.Substring(comma + 1, comma2 - (comma + 1)).Trim(); //tag name
                            t.Data = instr.Substring(comma2 + 1, end - (comma2 + 1)).Trim(); //data

                            //see if there is a number in the data
                            if (int.TryParse(t.Data, out d))
                            {
                                t.Value = d;
                                t.ValueValid = true;

                            }

                            Trace.WriteLine("Module, " + t.ModuleName + ", ");
                            Trace.WriteLine("Name, " + t.Name + ", ");
                            Trace.WriteLine("Data, " + t.Data + "\n");

                            //this works sort of
                            if (null != TagEvents)
                                TagEvents.Invoke(t);

                            //Uniques(t);

                        }
                        else
                        {
                            Trace.WriteLine("error, " + instr.Substring(offset) + "\n");

                            //if (null != TagErrorEvent)
                                //TagErrorEvent(instr.Substring(offset));
                        }
                    }
                    else
                    {
                        Trace.WriteLine("error1, " + instr.Substring(offset) + "\n");

                        //if (null != TagErrorEvent)
                            //TagErrorEvent(instr.Substring(offset));
                    }
                }
                else
                {
                    Trace.WriteLine("error2, " + instr.Substring(offset) + "\n");

                    //if (null != TagErrorEvent)
                        //TagErrorEvent(instr.Substring(offset));
                }
            }
            return end + 1;
        }

        private void Uniques(TagEvent e)
        {
            bool ModuleNameFound = false;

            //Search to see if tag exists
            foreach (Module m in ModuleList)
            {
                if (m.ModuleName == e.ModuleName)
                {
                    ModuleNameFound = true;

                    //module found, add tag or data if unique
                    m.Uniques(e);
                }
            }

            if (false == ModuleNameFound)
            {
                ModuleList.Add(new Module(e));

                ModuleList.Sort();

                //we have a new tag, redisplay them all
                //txtModules.Clear();

                foreach (Module m in ModuleList)
                {
                    //txtModules.AppendText(m.ModuleName + "\n");
                }

            }
        }
    }

    
}

