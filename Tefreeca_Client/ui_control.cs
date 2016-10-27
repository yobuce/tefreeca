using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Tefreeca_Client
{
    public enum Option_listBox { add, clear };
    public enum Option_datagridview { add, clear };
    public enum Option_listview { add, clear };
    public enum Option_button { text, enable };
    public enum Option_TextBox { text, enable };
    public enum Option_NumericUpDown { value, Maximum };
    public enum Option_TrackBar { value, Maximum, Minimum };
    
    class ui_control : Form //스레드 환경에서 안전하게 폼 컨트롤을 수정하는 코드.
    {
        
        public ui_control(Form target_form)
        {
            form = target_form;
        }
        private Form form;


        delegate void TrackBarCallback(TrackBar target, object value, Option_TrackBar option);
        public void TrackBardelegate(TrackBar target, object value, Option_TrackBar option)
        {
            if (target.InvokeRequired)
            {
                TrackBarCallback d = new TrackBarCallback(TrackBardelegate);
                form.Invoke(d, new object[] { target, value, option });
            }
            else
            {
                if ((Option_TrackBar)option == Option_TrackBar.Maximum)
                {
                    target.Maximum = (int)value;
                }
                else if ((Option_TrackBar)option == Option_TrackBar.value)
                {
                    target.Value = (int)value;
                }
                else if ((Option_TrackBar)option == Option_TrackBar.Minimum)
                {
                    target.Minimum = (int)value;
                }
            }
        }
        
        delegate void NumericUpDownCallback(NumericUpDown target, object value, Option_NumericUpDown option);
        public void NumericUpDowndelegate(NumericUpDown target, object value, Option_NumericUpDown option)
        {
            if (target.InvokeRequired)
            {
                NumericUpDownCallback d = new NumericUpDownCallback(NumericUpDowndelegate);
                form.Invoke(d, new object[] { target, value, option });
            }
            else
            {
                if ((Option_NumericUpDown)option == Option_NumericUpDown.Maximum)
                {
                    target.Maximum = (int)value;
                }
                else if ((Option_NumericUpDown)option == Option_NumericUpDown.value)
                {
                    target.Value = (int)value;
                }
            }
        }

        delegate void SetlistBoxCallback(ListBox target, string[] value, Option_listBox option);
        public void set_listBox_delegate(ListBox target, string[] value, Option_listBox option)
        {
            if (target.InvokeRequired)
            {
                SetlistBoxCallback d = new SetlistBoxCallback(set_listBox_delegate);
                form.Invoke(d, new object[] { target, value, option });
            }
            else
            {
                if (option == Option_listBox.add)
                {
                    foreach (string S in value)
                    {
                        if (target.Items.Count > 5)
                            target.Items.RemoveAt(0);
                        target.Items.Add(S);
                    }

                }
                else if (option == Option_listBox.clear)
                    target.Items.Clear();
            }
        }

        delegate void SetbuttonCallback(Button target, object value, Option_button option);
        public void set_button_delegate(Button target, object value, Option_button option)
        {
            if (target.InvokeRequired)
            {
                SetbuttonCallback d = new SetbuttonCallback(set_button_delegate);
                form.Invoke(d, new object[] { target, value, option });
            }
            else
            {
                if (option == Option_button.text)
                    target.Text = (string)value;
                else if (option == Option_button.enable)
                {
                    target.Enabled = (bool)value;
                }
            }
        }

        delegate void SetLabelCallback(Label target, string value);
        public void set_label_delegate(Label target, string value)
        {
            if (target.InvokeRequired)
            {
                SetLabelCallback d = new SetLabelCallback(set_label_delegate);
                form.Invoke(d, new object[] { target, value });
            }
            else
            {
                target.Text = value;
                target.Visible = true;
            }
        }
        public void set_label_delegate(Label target, string value, Color txtcolor)
        {
            if (target.InvokeRequired)
            {
                SetLabelCallback d = new SetLabelCallback(set_label_delegate);
                form.Invoke(d, new object[] { target, value });
            }
            else
            {
                target.Text = value;
                target.Visible = true;
                target.ForeColor = txtcolor;

                target.ForeColor = Color.Red;
            }
        }

        delegate void SetcheckboxCallback(CheckBox target, string value);
        public void set_checkbox_delegate(CheckBox target, string value)
        {
            if (target.InvokeRequired)
            {
                SetcheckboxCallback d = new SetcheckboxCallback(set_checkbox_delegate);
                form.Invoke(d, new object[] { target, value });
            }
            else
            {
                target.Text = value;
                target.Visible = true;
            }
        }
        
        delegate void SetDataGridViewCallback(DataGridView target, object[] value, Option_datagridview option);
        delegate void SetDataGridViewCallback_1(DataGridView target, object[] value, Option_datagridview option, Color row_color);
        public void set_DataGridView_delegate(DataGridView target, object[] value, Option_datagridview option)
        {
            set_DataGridView_delegate(target, value, option, Color.White);
        }
        public void set_DataGridView_delegate(DataGridView target, object[] value, Option_datagridview option, Color row_color)
        {
            if (target.InvokeRequired)
            {
                SetDataGridViewCallback_1 d = new SetDataGridViewCallback_1(set_DataGridView_delegate);
                form.Invoke(d, new object[] { target, value, option, row_color });
            }
            else
            {
                if (option == Option_datagridview.add)
                {
                    target.Rows.Add(value);
                    if (row_color != null)
                    {
                        for (int i = 0; i < target.Rows[target.Rows.Count - 1].Cells.Count; i++)
                        {
                            target.Rows[target.Rows.Count - 1].Cells[i].Style.BackColor = row_color;
                        }
                    }
                }
                else if (option == Option_datagridview.clear)
                    target.Rows.Clear();
            }
        }

        delegate void SetDataGridViewsortCallback(DataGridView target, DataGridViewColumn header);
        public void set_DataGridView_sortdelegate(DataGridView target, DataGridViewColumn header)
        {
            if (target.InvokeRequired)
            {
                SetDataGridViewsortCallback d = new SetDataGridViewsortCallback(set_DataGridView_sortdelegate);
                this.Invoke(d, new object[] { target, header });
            }
            else
            {
                target.Sort(header, ListSortDirection.Descending);
            }
        }

        
        delegate void SetlistviewCallback(ListView target, string[] value, Option_listview option);
        public void set_listview_delegate(ListView target, string[] value, Option_listview option)
        {
            if (target.InvokeRequired)
            {
                SetlistviewCallback d = new SetlistviewCallback(set_listview_delegate);
                form.Invoke(d, new object[] { target, value, option });
            }
            else
            {
                if (option == Option_listview.add)
                {
                    var strArray = value;
                    var lvt = new ListViewItem(strArray);
                    target.Items.Add(lvt);
                }
                else if (option == Option_listview.clear)
                    target.Items.Clear();
            }
        }

        delegate void tabpageCallback(TabPage target, string value);
        public void set_tabpage_delegate(TabPage target, string value)
        {
            if (target.InvokeRequired)
            {
                tabpageCallback d = new tabpageCallback(set_tabpage_delegate);
                form.Invoke(d, new object[] { target, value });
            }
            else
            {
                target.Text = value;
            }
        }

        delegate void SetPanelVisibleCallback(Panel target, bool visible);
        public void set_PanelVisible_delegate(Panel target, bool visible)
        {
            if (target.InvokeRequired)
            {
                SetPanelVisibleCallback d = new SetPanelVisibleCallback(set_PanelVisible_delegate);
                form.Invoke(d, new object[] { target, visible });
            }
            else
            {
                target.Visible = visible;
            }
        }


        delegate void SetTextBoxCallback(TextBox target, object value, Option_TextBox option);
        public void set_TextBox_delegate(TextBox target, object value, Option_TextBox option)
        {
            if (target.InvokeRequired)
            {
                SetTextBoxCallback d = new SetTextBoxCallback(set_TextBox_delegate);
                form.Invoke(d, new object[] { target, value, option });
            }
            else
                if (option == Option_TextBox.enable)
                    target.Enabled = (bool)value;
                else if (option == Option_TextBox.text)
                    target.Text = (string)value;
        }
    }
}
