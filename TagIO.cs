//Mettle, an embedded software analysis tool
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

namespace MettleLib
{
    public partial class TagIO : Control, ITagInterface
    {
        private string m_ModuleName;
        public delegate void InvokeDelegate(bool c);

        public TagIO()
        {
            InitializeComponent();
        }

#region Interfaces

        void ITagInterface.Reset()
        {
            Checked = false;
        }

        void ITagInterface.Initialize()
        {
        }

        void ITagInterface.UpdateEvent(TagEvent e)
        {
            bool ckd;

            if ((Module == null) || (Module == e.ModuleName))
            {
                if (e.Name == base.Tag.ToString())
                {
                    if (e.Value == 1)
                        ckd = true;
                    else
                        ckd = false;

                    this.BeginInvoke(new InvokeDelegate(TagInvoke), ckd);
                }
            }
        }

        public void TagInvoke(bool c)
        {
            Checked = c;
        }

#endregion
        public new string Text
        {
            get { return base.Text; }

            set
            {
                if (value == base.Text)
                {
                    return;
                }

                base.Text = value;

                Invalidate();
            }
        }
        private bool _Checked = false;
        public bool Checked
        {
            get { return _Checked; }
            set
            {
                if (value == _Checked)
                    return;

                _Checked = value;

                Invalidate();
            }
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics gfx = e.Graphics;
            Rectangle rc = ClientRectangle;

            rc.Width -= 1;
            rc.Height -= 1;

            gfx.FillRectangle(new SolidBrush(Parent.BackColor), ClientRectangle);

            Color fill;
            if (Checked)
                fill = base.ForeColor; //Color.Blue;
            else
                fill = base.BackColor; // Color.LightGray;

            gfx.FillEllipse(new SolidBrush(fill), rc);

            gfx.DrawEllipse(new Pen(Color.Blue, 1.0f), rc);

            Font fnt = new Font("Verdana", (float)rc.Height * 0.4f, FontStyle.Bold, GraphicsUnit.Pixel);

            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;
            gfx.DrawString(Text, fnt, new SolidBrush(Color.Black), new RectangleF((float)rc.Left, (float)rc.Top, (float)rc.Width, (float)rc.Height), sf);

        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            base.OnPaintBackground(pevent);
        }


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
        string dummy;

        [System.ComponentModel.Browsable(true),
        System.ComponentModel.Category("Mettle"),
        System.ComponentModel.Description("The Sort name filter (AKA tag). Leave blank to see all Sorts for this module")]
        public string Sort
        {
            get
            {
                if (this.Site == null || !this.Site.DesignMode)
                {
                    // Not in design mode, okay to do dangerous stuff...
                    return base.Tag.ToString();
                }
                else
                {
                    return dummy;
                }
            }
            set
            {
                if (this.Site == null || !this.Site.DesignMode)
                {
                    base.Tag = value;
                }
                else
                {
                    dummy = value;
                }
            }
        }

        UInt16 m_Mask = 0;

        [System.ComponentModel.Browsable(true),
        System.ComponentModel.Category("Mettle"),
        System.ComponentModel.Description("The Mask for this IO bit). If this value logically anded with the data >0 the button is highlighted")]
        public UInt16 U16_Mask
        {
            get
            {
                return m_Mask;
            }
            set
            {
                m_Mask = value;
            }
        }
    }
}
