﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace MettleLib
{
    public partial class TagError : TextBox, ITagErrorInterface
    {
        private const int EM_SETTABSTOPS = 0x00CB;
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr h, int msg, int wParam, int[] lParam);
        private const int TabSize = 4;

        private string m_ModuleName;
        public delegate void InvokeDelegate(string s);

        public TagError()
        {
            InitializeComponent();
        }

        #region Interfaces

        void ITagErrorInterface.Reset()
        {
            Clear();
        }

        void ITagErrorInterface.Initialize()
        {
            // define value of the Tab indent 
            int[] stops = { TabSize };
            // change the indent 
            //SendMessage(this.Handle, EM_SETTABSTOPS, 1, stops);
            SendMessage(Handle, EM_SETTABSTOPS, 1, new int[] { TabSize * 4 });
        }

        void ITagErrorInterface.UpdateEvent(string s)
        {
            this.BeginInvoke(new InvokeDelegate(TagInvoke), s + "\r\n");
            //AppendText(s + "\r\n");
        }


        //Update the control in the GUI thread context
        public void TagInvoke(string s)
        {
            this.AppendText(s);
            ScrollToCaret();
        }
        #endregion

        [System.ComponentModel.Browsable(true),
        System.ComponentModel.Category("Mettle"),
        System.ComponentModel.Description("The module name filter. Leave blank to see all module")]
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
                return base.Tag.ToString();
            }
            set
            {
                base.Tag = value;
            }
        }
    }
}
