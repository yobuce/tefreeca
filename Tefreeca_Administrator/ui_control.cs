using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Tefreeca_Administrator
{
    public enum Option_listBox { add, clear };
    public enum Option_datagridview { add, clear };
    public enum Option_listview { add, clear, delete, update };
    public enum Option_button { text, enable };
    public enum Option_TextBox { text, enable };
    public enum Option_NumericUpDown { value, Maximum };
    public enum Option_TrackBar { value, Maximum, Minimum };
    public enum Option_Audo_listview { Auto_display };
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
                else if (option == Option_listview.update) //업데이트 일 경우 맨 첫 칼럼을 키로 잡아 검색하여, 걸리면, 나머지를 string[]로 수정한다.
                {
                    for (int i = 0; i < target.Items.Count; i++)
                    {
                        if (target.Items[i].SubItems[0].Text == value[0])
                        {
                            for (int j = 1; j < value.Length; j++)
                            {
                                if (value[j] == null)
                                    continue;
                                if (value[j] == "")
                                    continue;
                                target.Items[i].SubItems[j].Text = value[j];
                            }
                            break;
                        }
                    }
                }
                else if (option == Option_listview.delete)
                {
                    for (int i = 0; i < target.Items.Count; i++)
                    {
                        if (target.Items[i].SubItems[0].Text == value[0])
                        {
                            target.Items[i].Remove();
                            break;
                        }
                    }
                }
            }
        }
        //칼럼의 첫번째 값을 키로 하여 자동적으로 추가 수정 삭제를 수행하는 코드.
        delegate void SetAutolistviewCallback(ListView target, List<string[]> value, Option_Audo_listview option);
        public void set_Autolistview_delegate(ListView target, List<string[]> value, Option_Audo_listview option)
        {
            if (target.InvokeRequired)
            {
                SetAutolistviewCallback d = new SetAutolistviewCallback(set_Autolistview_delegate);
                form.Invoke(d, new object[] { target, value, option });
            }
            else
            {
                if (value.Count == 0)
                {
                    target.Items.Clear();
                    return;
                }
                int xsize = value[0].Length;

                for (int y = 0; y < target.Items.Count; y++)
                {
                    string key = target.Items[y].SubItems[0].Text;
                    bool find_ = false;

                    for (int s = 0; s < value.Count; s++)
                    {
                        if (value[s][0] == key) //서브에서 리스트와 같은 키를 찾으면
                        {
                            find_ = true;
                            for (int x = 0; x < xsize; x++) //변동이 있는지 검사하여
                            {
                                if (target.Items[y].SubItems[x].Text != value[s][x])
                                {
                                    target.Items[y].SubItems[x].Text = value[s][x].ToString(); //바뀐부분만 업데이트.
                                }
                            }
                            value.RemoveAt(s);
                            break;
                        }
                    }
                    if(!find_) //새로운 리스트에 기존항목이 없다면 삭제된 것이므로 삭제,
                        target.Items[y].Remove();
                } //여기까지 삭제와 업데이트
                  //지금부턴 추가.
                  //(처리 되지 않은 엔트리는 리스트뷰에 아직 없기 때문에 추가) 
                foreach (string[] L in value)
                {
                    var strArray = L;
                    var lvt = new ListViewItem(strArray);
                    target.Items.Add(lvt);
                }
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
