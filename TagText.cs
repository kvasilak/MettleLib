﻿//Mettle, an embedded software analysis tool
//Copyright (C) 2013  Keith Vasilakes
//
//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.
//
//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.
//
//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MettleLib
{
    public partial class TagText : TextBox, ITagInterface
    {
        private const int EM_SETTABSTOPS = 0x00CB;
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr h, int msg, int wParam, int[] lParam);
        private const int TabSize = 4;

        //Property variables
        private string m_ModuleName;
        private string m_Sort;
        private bool UsetimeStamp = false;
        
        public delegate void InvokeDelegate(string s);


        public TagText()
        {
            InitializeComponent();
        }


        #region Interfaces

        void ITagInterface.Reset()
        {
            Clear();
        }

        //Do any custom initialization here
        void ITagInterface.Initialize()
        {
            // define value of the Tab indent 
            int[] stops = { TabSize };
            // change the indent 
            //SendMessage(this.Handle, EM_SETTABSTOPS, 1, stops);
            SendMessage(Handle, EM_SETTABSTOPS, 1, new int[] { TabSize * 4 });
        }

        void ITagInterface.UpdateEvent(TagEvent e)
        {
            string txt;

            if ((Module == null) || (Module == e.ModuleName))
            {
                if (e.Name == Sort)
                {
                    if (base.Multiline)
                    {
                        try
                        {
                            this.BeginInvoke(new InvokeDelegate(TagInvoke), e.Data + "\r\n");
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine("TagText1, " + ex.Message + "\n");
                        }
                    }
                    else
                    {
                        this.BeginInvoke(new InvokeDelegate(TagInvoke2), e.Data);
                    }
                }

                if (Sort == "*")
                {
                    try
                    {
                        if (base.Multiline)
                        {
                            if (e.Name.Length < TabSize)
                            {
                                txt = e.Name + "\t\t\t\t" + e.Data + "\r\n";
                            }
                            else if (e.Name.Length < TabSize * 2)
                            {
                                txt = e.Name + "\t\t\t" + e.Data + "\r\n";
                            }
                            else if (e.Name.Length < TabSize * 3)
                            {
                                txt = e.Name + "\t\t" + e.Data + "\r\n";
                            }
                            else
                            {
                                txt = e.Name + "\t" + e.Data + "\r\n";
                            }

                            //were in the serial ports thread, update the thread in the GUI context
                            this.BeginInvoke(new InvokeDelegate(TagInvoke), txt);
                        }
                        else
                        {
                            txt = e.Name + "\t\t" + e.Data + "\r\n";
                            this.BeginInvoke(new InvokeDelegate(TagInvoke2), txt);
                        }

                        
                        
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("TagText, " + ex.Message + "\n");
                    }
                }
            }
        }

        //Update the control in the GUI thread context
        //single like text box
        public void TagInvoke2(string s)
        {
            this.Text = s;
        }

        //Update the control in the GUI thread context
        //Multiline text box
        public void TagInvoke(string s)
        {
            if (UsetimeStamp)
            {
                //this.AppendText(DateTime.Now.ToLongTimeString() + "; ");
                this.AppendText(DateTime.Now.ToString("HH:mm:ss; "));
            }
            this.AppendText(s);
            ScrollToCaret();
        }

        #endregion

        [System.ComponentModel.Browsable(true),
        System.ComponentModel.Category("Mettle"),
        System.ComponentModel.Description("The module filter. Leave blank to see all modules")]
        public string Module
        {
            get
            {
                return m_ModuleName;
            }
            set
            {
                m_ModuleName = value;
            }
        }

        [System.ComponentModel.Browsable(true),
        System.ComponentModel.Category("Mettle"),
        System.ComponentModel.Description("The Sort name filter (AKA tag). Leave blank to see all Sorts for this module")]
        public string Sort
        {
            get
            {
                return m_Sort;
            }
            set
            {
                m_Sort = value; 
            }
        }

        [System.ComponentModel.Browsable(true),
        System.ComponentModel.Category("Mettle"),
        System.ComponentModel.Description("Display a HH:MM:SS timestamp for each line")]
        public bool Timestamp
        {
            get
            {
                return UsetimeStamp;
            }
            set
            {
                UsetimeStamp = value;
            }
        }
    }
}
