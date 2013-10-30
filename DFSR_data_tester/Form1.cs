// threading http://www.albahari.com/threading/part3.aspx
// http://stackoverflow.com/questions/368006/c-can-i-add-values-to-a-listbox-with-a-backgroundwork-thread
// final for backgroundworker: http://www.dotnetperls.com/backgroundworker
// http://www.dotnetperls.com/progressbar


// filestream object versus bufferedstream: http://blogs.msdn.com/b/brada/archive/2004/04/15/114329.aspx
// filestream and buffering http://stackoverflow.com/questions/122362/how-to-empty-flush-windows-read-disk-cache-in-c

// file truncation: http://blogs.msdn.com/b/oldnewthing/archive/2010/12/01/10097859.aspx
// http://www.codeproject.com/KB/cs/InsertTextInCSharp.aspx


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;


namespace DFSR_data_tester
{
    public partial class Form1 : Form
    {


        string filename;

        public Form1()
        {
            InitializeComponent();
        }

        private void _bw_DoWork(object sender, DoWorkEventArgs e)
        {
            //break up and assign the arguments passed by the methods:
            string[] CommandArguments = e.Argument.ToString().Split(':');
            string CommandOperation = CommandArguments[0];
            int CommandOperationTotalSize = Int32.Parse(CommandArguments[1]);
            int CommandOperationBufferSize = Int32.Parse(CommandArguments[2]);


            //create a file

            if (CommandOperation == "create")
            {

                try
                {
                    BinaryWriter binWriter = new BinaryWriter(File.Open(filename, FileMode.Create));
                    binWriter.Close(); //close calls dispose

                    e.Result = "Created 0KB file " + filename;
                    _bw.ReportProgress(100);
                }
                catch (Exception ex)
                {
                    e.Result = "with EXCEPTION: " + ex.Message;
                    Console.Beep(500, 100);
                    _bw.ReportProgress(0);
                }


            }


            //write to a file
            if (CommandOperation == "write" || CommandOperation == "append" || CommandOperation == "writewait" || CommandOperation == "appendwait")
            {
                try
                {
                    //use the FileStream buffer to actually buffer the data to be written, so segments are written as desired.
                    FileStream writeStream = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.None, CommandOperationBufferSize);
                    BinaryWriter binWriter = new BinaryWriter(writeStream);

                    //VERSION 1: In this version, the FileStream will flush data to the file when the buffer is full, automatically
                    if (CommandOperation == "write" || CommandOperation == "append")
                    {
                        byte[] FullSize = new byte[CommandOperationTotalSize];

                        //the BinaryWriter will flush when the FileStream buffer is hit
                        binWriter.Write(FullSize);
                        _bw.ReportProgress(20);
                    }

                    //VERSION 2: In this version, the FileStreamm will only flush data on flush
                    if (CommandOperation == "writewait" || CommandOperation == "appendwait")
                    {
                        //the BinaryWriter will "automatically" .Flush() when the FileStream buffer is full
                        byte[] ChunkSize = new byte[CommandOperationBufferSize - 1];

                        int i = CommandOperationBufferSize;

                        do
                        {
                            binWriter.Write(ChunkSize);
                            writeStream.Flush();
                            i = i + CommandOperationBufferSize;
                        }
                        while (i < CommandOperationTotalSize);
                        _bw.ReportProgress(80);
                    }

                    writeStream.Close();
                    writeStream.Dispose();
                    _bw.ReportProgress(90);
                    binWriter.Close();
                    _bw.ReportProgress(100);
                    e.Result = "Write/Append " + (CommandOperationTotalSize / 1024 / 1024) + "MB in " + (CommandOperationBufferSize / 1024) + "KB buffered chunks " + filename;
                }
                catch (Exception ex)
                {
                    e.Result = "with EXCEPTION: " + ex.Message;
                    Console.Beep(500, 100);
                }
            }

            //read from file
            if (CommandOperation == "read")
            {
                try
                {

                    //read data directly from the file (when writing, we have to generate the data we write)
                    FileStream readStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None, CommandOperationBufferSize);
                    BinaryReader binReader = new BinaryReader(readStream);

                    int peekchar = binReader.PeekChar();
                    if (peekchar <= -1)
                    {
                        e.Result = filename + " is empty.";
                    }
                    else
                    {
                        int i = CommandOperationBufferSize;
                        int progbarstepsize = 100 / (100 / (CommandOperationTotalSize / CommandOperationBufferSize));

                        while (i < CommandOperationTotalSize)
                        {
                            binReader.ReadBytes(CommandOperationBufferSize);
                            i = i + CommandOperationBufferSize;
                            _bw.ReportProgress(progbarstepsize + 1000);
                        }

                        e.Result = "Read " + (CommandOperationTotalSize / 1024 / 1024) + "MB in " + (CommandOperationBufferSize / 1024) + "KB buffered chunks " + filename;
                    }

                    readStream.Close();
                    readStream.Dispose();
                    _bw.ReportProgress(90);
                    binReader.Close();
                    _bw.ReportProgress(100);

                }
                catch (Exception ex)
                {
                    e.Result = "with EXCEPTION: " + ex.Message;
                    Console.Beep(500, 100);
                }
            }

            //truncate file
            if (CommandOperation == "truncate")
                try
                {
                    FileStream writeStream = new FileStream(filename, FileMode.Truncate);
                    _bw.ReportProgress(80);
                    writeStream.Close();
                    _bw.ReportProgress(90);
                    writeStream.Dispose();
                    _bw.ReportProgress(100);
                    e.Result = "Truncated " + filename;
                }
                catch (Exception ex)
                {
                    e.Result = "with EXCEPTION: " + ex.Message;
                    Console.Beep(500, 100);
                }

            //delete file
            if (CommandOperation == "delete")
                if (System.IO.File.Exists(filename))
                {
                    System.IO.File.Delete(filename);
                    _bw.ReportProgress(100);
                    e.Result = "Delete " + filename;
                }
                else
                {
                    e.Result = "with EXCEPTION: " + filename + " file doesn't exist";
                    Console.Beep(500, 100);
                }
        }


        private void _bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            textBox1.Text = "Last operation finished, refer to last entry in log";
            listBox1.Items.Add(DateTime.Now.ToString("HH:mm:ss.fff") + " END: " + e.Result);
            listBox1.SetSelected(listBox1.Items.Count - 1, true);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //display the file dialog to create a new file
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Save file as...";
            dialog.Filter = "All files (*.*)|*.*";
            dialog.RestoreDirectory = true;
            //dialog.InitialDirectory = "c:\\";
            dialog.FileName = @"~filetest.tmp";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                filename = dialog.FileName;
                textBox1.Text = "Create 0KB file";
                listBox1.Items.Add(DateTime.Now.ToString("HH:mm:ss.fff") + " START: Create 0KB file");

                _bw.RunWorkerAsync("create:0:0");
            }

            //enable all of the other buttons
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            button5.Enabled = true;
            button6.Enabled = true;
            button7.Enabled = true;
            button8.Enabled = true;
            button9.Enabled = true;
            button12.Enabled = true;
            button6.Enabled = true;



        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Text = "Write " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB in " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB buffered chunks";
            listBox1.Items.Add(DateTime.Now.ToString("HH:mm:ss.fff") + " START: Write " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB in " + Int32.Parse(textBox3.Text.Substring(0, textBox3.Text.Length - 2)) + "KB buffered chunks " + filename);

            _bw.RunWorkerAsync("write:" + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) * 1024 + ":" + Int32.Parse(textBox3.Text.Substring(0, textBox3.Text.Length - 2)) * 1024);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox1.Text = "Read" + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB in " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB buffered chunks";
            listBox1.Items.Add(DateTime.Now.ToString("HH:mm:ss.fff.fff") + " START: Read " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB in " + Int32.Parse(textBox3.Text.Substring(0, textBox3.Text.Length - 2)) + "KB buffered chunks " + filename);

            _bw.RunWorkerAsync("read:" + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) * 1024 + ":" + Int32.Parse(textBox3.Text.Substring(0, textBox3.Text.Length - 2)) * 1024);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox1.Text = "Append " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB in " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB buffered chunks";
            listBox1.Items.Add(DateTime.Now.ToString("HH:mm:ss.fff") + " START: Append " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB in " + Int32.Parse(textBox3.Text.Substring(0, textBox3.Text.Length - 2)) + "KB buffered chunks " + filename);

            _bw.RunWorkerAsync("append:" + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) * 1024 + ":" + Int32.Parse(textBox3.Text.Substring(0, textBox3.Text.Length - 2)) * 1024);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            //write
            textBox1.Text = "Write " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB in " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB buffered chunks";
            listBox1.Items.Add(DateTime.Now.ToString("HH:mm:ss.fff") + " START: Write 1MB in 1MB buffered chunks " + filename);

            _bw.RunWorkerAsync("write:" + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) * 1024 + ":" + Int32.Parse(textBox3.Text.Substring(0, textBox3.Text.Length - 2)) * 1024);
        }


        private void button8_Click(object sender, EventArgs e)
        {
            //read 2MB in 2MB chunk
            textBox1.Text = "Read " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB in " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB buffered chunks";
            listBox1.Items.Add(DateTime.Now.ToString("HH:mm:ss.fff") + " START: Read " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB in " + Int32.Parse(textBox3.Text.Substring(0, textBox3.Text.Length - 2)) + "KB buffered chunks " + filename);

            _bw.RunWorkerAsync("read:" + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) * 1024 + ":" + Int32.Parse(textBox3.Text.Substring(0, textBox3.Text.Length - 2)) * 1024);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //delete all data from file / truncate
            textBox1.Text = "Truncate file";
            listBox1.Items.Add(DateTime.Now.ToString("HH:mm:ss.fff") + " START: Truncate " + filename);

            _bw.RunWorkerAsync("truncate:0:0");
        }

        private void button12_Click(object sender, EventArgs e)
        {
            //delete file
            textBox1.Text = "Delete file";
            listBox1.Items.Add(DateTime.Now.ToString("HH:mm:ss.fff") + " START: Delete " + filename);

            _bw.RunWorkerAsync("delete:0:0");

        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        ToolTip explanation = new ToolTip();

        private void linkLabel1_MouseEnter(object sender, EventArgs e)
        {
            explanation.InitialDelay = 0;
            explanation.Show("This is used to generate certain types of I/O to a give file.\nPartner this with something like Process Monitor's File Summary tool, to monitor specific I/O.\nI used this to monitor what DFS-R was doing with the dfsrprivatedata folder.", linkLabel1, 10000);
        }

        private void linkLabel1_MouseLeave(object sender, EventArgs e)
        {
            explanation.Hide(linkLabel1);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            textBox2.Text = (trackBar1.Value * 64).ToString() + "KB";

            int textbox2value = Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2));
            int textbox3value = Int32.Parse(textBox3.Text.Substring(0, textBox3.Text.Length - 2));

            if (textbox2value <= textbox3value)
            {
                trackBar2.Value = trackBar1.Value;
                textBox3.Text = (trackBar2.Value * 64).ToString() + "KB";

            }

        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            textBox3.Text = (trackBar2.Value * 64).ToString() + "KB";

            int textbox2value = Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2));
            int textbox3value = Int32.Parse(textBox3.Text.Substring(0, textBox3.Text.Length - 2));

            if (textbox2value <= textbox3value)
            {
                trackBar1.Value = trackBar2.Value;
                textBox2.Text = (trackBar1.Value * 64).ToString() + "KB";

            }

        }

        private void button9_Click(object sender, EventArgs e)
        {
            //Write, wait, then flush, then wait.

            // feature tool tip to say most effective when data total data is above chunk size.
            textBox1.Text = "Write " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB in " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB buffered chunks, wait, flush, wait";
            listBox1.Items.Add(DateTime.Now.ToString("HH:mm:ss.fff") + " START: Write " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB in " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB buffered chunks, wait five seconds, call a flush, wait five seconds, close " + filename);

            _bw.RunWorkerAsync("writewait:" + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) * 1024 + ":" + Int32.Parse(textBox3.Text.Substring(0, textBox3.Text.Length - 2)) * 1024);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //Append, wait, then flush, then wait.
            textBox1.Text = "Append " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB in " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB buffered chunks, wait, flush, wait";
            listBox1.Items.Add(DateTime.Now.ToString("HH:mm:ss.fff") + " START: Append " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB in " + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) + "KB buffered chunks, wait five seconds, call a flush, wait five seconds, close " + filename);

            _bw.RunWorkerAsync("appendwait:" + Int32.Parse(textBox2.Text.Substring(0, textBox2.Text.Length - 2)) * 1024 + ":" + Int32.Parse(textBox3.Text.Substring(0, textBox3.Text.Length - 2)) * 1024);

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            explanation.InitialDelay = 0;
            explanation.Show("This is used to generate certain types of I/O to a give file.\nPartner this with something like Process Monitor's File Summary tool, to monitor specific I/O.\nI used this to monitor what DFS-R was doing with the dfsrprivatedata folder.", linkLabel1, 10000);
        }

        private void trackBar1_MouseDown(object sender, MouseEventArgs e)
        {
            ToolTip PageUpDownAlert = new ToolTip();
            PageUpDownAlert.InitialDelay = 0;
            PageUpDownAlert.Show("Use page up/down to adjust by the MB.", trackBar1, trackBar1.Left + 100, trackBar1.Top - 400, 3000);
        }

        private void _bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (progressBar1.Value == 100)
            {
                progressBar1.Value = 0;
            }

            if (e.ProgressPercentage > 1000)
            //was sent by calculated progbarstepsize
            {
                try
                {
                    int progbarstepsize = e.ProgressPercentage - 1000;
                    if ((progressBar1.Value + progbarstepsize) > 100)
                    {
                        progressBar1.Value = 100;
                    }
                    else
                    {
                        progressBar1.Value = progressBar1.Value + progbarstepsize;
                    }
                }
                catch (Exception ex)
                {
                    listBox1.Items.Add(ex.Message);
                }
            }
            else
            {
                progressBar1.Value = e.ProgressPercentage;
            }
        }

        private void textBox2_Leave(object sender, EventArgs e)
        {
            if (textBox2.Text.Contains("KB"))
            {
                //nada
            }
            else
            {

                try
                {
                    //check if number
                    int i;
                    if (int.TryParse(textBox2.Text, out i))
                    {
                        textBox2.Text = i.ToString() + "KB";
                    }
                    else
                    {
                        textBox2.Text = "";
                    }
                }
                catch
                {
                    textBox2.Text = "";
                }

            }
        }

        private void textBox3_Leave(object sender, EventArgs e)
        {
            if (textBox3.Text.Contains("KB"))
            {
                //nada
            }
            else
            {

                try
                {
                    //check if number
                    int i;
                    if (int.TryParse(textBox3.Text, out i))
                    {
                        textBox3.Text = i.ToString() + "KB";
                    }
                    else
                    {
                        textBox3.Text = "";
                    }
                }
                catch
                {
                    textBox3.Text = "";
                }
            }
        }



    }
}
