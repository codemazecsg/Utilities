using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Collections;

namespace dfschema
{
    public partial class Form1 : Form
    {
        // globals
        SqlConnection cn;

        public Form1()
        {
            InitializeComponent();
        }

        private void BtnLogon_Click(object sender, EventArgs e)
        {

            if (txtServerName.Text.Length == 0 || txtPassword.Text.Length == 0 || txtDatabaseName.Text.Length == 0)
            {
                MessageBox.Show("You must provide all logon details to connect.", "Logon Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // build the connection string
            SqlConnectionStringBuilder cnString = new SqlConnectionStringBuilder();

            cnString.InitialCatalog = txtDatabaseName.Text;
            cnString.DataSource = txtServerName.Text;
            cnString.Password = txtPassword.Text;
            cnString.UserID = txtUserName.Text;

            // get connection opened

            try
            {
                // init
                cn = new SqlConnection(cnString.ToString());

                // open
                cn.Open();

                // if we get here, disable
                grpLogon.Enabled = false;

                // enable table selection
                grpTables.Enabled = true;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString(), "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // get tables
            getTableListing();

        }

        private void getTableListing()
        {
            // clear
            lsvTables.Items.Clear();

            // query to get tables
            string sqlString = @"select schema_name(schema_id) as 'schema', [name] from sys.tables where type = 'U' order by schema_id asc;";

            // cmd object to send to SQL
            SqlCommand cmd = new SqlCommand(sqlString, cn);

            // record set
            SqlDataReader dr = null;

            try
            {
                dr = cmd.ExecuteReader();

                // add to lsv
                while(dr.Read())
                {
                    // add col 1
                    ListViewItem lvi = new ListViewItem(dr[0].ToString());

                    // add col 2
                    lvi.SubItems.Add(dr[1].ToString());

                    // add to lsv
                    lsvTables.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString(), "Get Tables Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                dr.Close();
            }
                
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            lsbImports.Items.Add("from pyspark.sql.types import *");
            btnAddImport.Enabled = false;
        }

        private void BtnAddImport_Click(object sender, EventArgs e)
        {
            if (txtImport.Text.Length > 0)
            {
                lsbImports.Items.Add(txtImport.Text);
            }
            else
            {
                MessageBox.Show("You must provide and import statement", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtImport_TextChanged(object sender, EventArgs e)
        {
            if (txtImport.Text.Length > 0)
            {
                btnAddImport.Enabled = true;
            }
            else
            {
                btnAddImport.Enabled = false;
            }
        }

        private void LsbImports_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lsbImports.SelectedItems.Count >= 1)
            {
                btnDelete.Enabled = true;
            }
            else
            {
                btnDelete.Enabled = false;
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            lsbImports.Items.Remove(lsbImports.SelectedItem);
        }

        private void LsvTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lsvTables.SelectedItems.Count > 0)
            {
                grpImports.Enabled = true;
                grpOptions.Enabled = true;
                btnBuild.Enabled = true;
                btnRemove.Enabled = true;
                chkIncludeSaveAsTable.Enabled = true;
            }
            else
            {
                grpImports.Enabled = false;
                grpOptions.Enabled = false;
                btnBuild.Enabled = false;
                btnRemove.Enabled = false;
                chkIncludeSaveAsTable.Enabled = false;
            }
        }

        private void LsvTables_DoubleClick(object sender, EventArgs e)
        {
            lsvTables.SelectedItems.Clear();
        }

        private void BtnBuild_Click(object sender, EventArgs e)
        {
            // query to get tables
            string sqlString = String.Format(@"select
	                                                c.name,
	                                                x.name,
                                                    c.precision,
                                                    c.scale,
	                                                c.system_type_id
                                               from sys.columns c 
	                                                inner join sys.tables t
	                                                on c.object_id = t.object_id
	                                                inner join sys.types x
	                                                on c.system_type_id = x.system_type_id
	                                                where t.[object_id] = object_id('{1}.{0}')
	                                                and t.[schema_id] = schema_id('{1}')
	                                                order by c.column_id asc;",
                                                    lsvTables.SelectedItems[0].SubItems[1].Text,
                                                    lsvTables.SelectedItems[0].Text);

            // cmd object to send to SQL
            SqlCommand cmd = new SqlCommand(sqlString, cn);

            // record set
            SqlDataReader dr = null;

            StringBuilder cols = new StringBuilder();
            StringBuilder body = new StringBuilder();

            // init cols
            cols.Append("\t\t");
            cols.Append("[");

            try
            {
                int cnt = 0;
                dr = cmd.ExecuteReader();

                while(dr.Read())
                {
                    if (cnt > 0)
                    {
                        cols.Append(",");
                        cols.Append(Environment.NewLine);
                        cols.Append("\t\t");
                    }

                    cols.Append("StructField(");
                    cols.Append("\"");
                    cols.Append(dr[0].ToString());
                    cols.Append("\"");
                    cols.Append(", ");

                    string dType = dr[1].ToString();
                    string dPrecision = dr[2].ToString();
                    string dScale = dr[3].ToString();
                    string dTag = "!!!UNKNOWN!!!";

                    switch (dType)
                    {
                        case "int":
                            dTag = "IntegerType()";
                            break;
                        case "varchar":
                            dTag = "StringType()";
                            break;
                        case "datetime":
                            dTag = "DateType()";
                            break;
                        case "date":
                            dTag = "DateType()";
                            break;
                        case "uniqueidentifier":
                            dTag = "StringType()";
                            break;
                        case "datetime2":
                            dTag = "TimestampType()";
                            break;
                        case "smalldatetime":
                            dTag = "DateType()";
                            break;
                        case "tinyint":
                            dTag = "ByteType()";
                            break;
                        case "smallint":
                            dTag = "ShortType()";
                            break;
                        case "real":
                            dTag = "FloatType()";
                            break;
                        case "float":
                            dTag = "FloatType()";
                            break;
                        case "bit":
                            dTag = "BooleanType()";
                            break;
                        case "decimal":
                            dTag = String.Format(@"DecimalType({0},{1})", dPrecision.ToString(), dScale.ToString());
                            break;
                        case "numeric":
                            dTag = String.Format(@"DecimalType({0},{1})", dPrecision.ToString(), dScale.ToString());
                            break;
                        case "smallmoney":
                            dTag = String.Format(@"DecimalType({0},{1})", dPrecision.ToString(), dScale.ToString());
                            break;
                        case "bigint":
                            dTag = "LongType()";
                            break;
                        case "char":
                            dTag = "StringType()";
                            break;
                        case "nvarchar":
                            dTag = "StringType()";
                            break;
                        case "nchar":
                            dTag = "StringType()";
                            break;
                        default:
                            dTag = "!!!UNKNOWN!!!";
                            break;
                    }

                    cols.Append(dTag);
                    cols.Append(")");

                    // increment
                    cnt++;

                }

                // close cols
                cols.Append("]");

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString(), "Builder Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                dr.Close();
            }

            // build body
            foreach (string s in lsbImports.Items)
            {
                body.Append(s.ToString());
                body.Append(Environment.NewLine);
            }

            // add some spacers
            body.Append(Environment.NewLine);

            body.Append("tableSchema = StructType(");
            body.Append(Environment.NewLine);
            body.Append(cols.ToString());
            body.Append(")");

            // ad some spacers
            body.Append(Environment.NewLine);
            body.Append(Environment.NewLine);

            if (txtFilePath.Text.Length > 0)
            {
                body.Append("df = spark.read.csv(");
                body.Append("\"");
                body.Append(txtFilePath.Text.ToString());
                body.Append("\"");
                body.Append(", ");

                // file contains headers?
                if (chkHeader.Checked)
                {
                    body.Append("header=True, ");
                }
                else
                {
                    body.Append("header=False, ");
                }

                if (txtSeparator.Text.Length > 0)
                {
                    body.Append("sep=");
                    body.Append("\"");
                    body.Append(txtSeparator.Text);
                    body.Append("\"");
                    body.Append(", ");
                }

                // date format?
                if (txtDateFormat.Text.Length > 0)
                {
                    body.Append("dateFormat=");
                    body.Append("\"");
                    body.Append(txtDateFormat.Text);
                    body.Append("\"");
                    body.Append(", ");
                }

                if (txtNullValues.Text.Length > 0)
                {
                    body.Append("nullValue=");
                    body.Append("\"");
                    body.Append(txtNullValues.Text);
                    body.Append("\"");
                    body.Append(", ");
                }

                body.Append("inferSchema=False, ");
                body.Append("schema=tableSchema)");
                body.Append(Environment.NewLine);
            }
            else
            {
                body.Append("df = spark.createDataFrame(");
                body.Append("[], ");
                body.Append("schema=tableSchema)");
                body.Append(Environment.NewLine);
            }

            if (chkIncludeSaveAsTable.Checked)
            {
                if (chkDeltaTable.Checked)
                {
                    body.Append(Environment.NewLine);
                    body.Append("df.write.format(");
                    body.Append("\"");
                    body.Append("delta");
                    body.Append("\")");
                    body.Append(".save(");
                    body.Append("\"");
                    body.Append(txtDeltaTablePath.Text);
                    body.Append("\"");
                    body.Append(")");

                    body.Append(Environment.NewLine);
                    body.Append("spark.sql(\"");
                    body.Append("create table ");

                    if (txtDatabaseSave.Text.Length > 0)
                    {
                        body.Append(txtDatabaseSave.Text);
                        body.Append(".");
                    }

                    body.Append(lsvTables.SelectedItems[0].SubItems[1].Text);
                    body.Append(" ");
                    body.Append("using delta location '");
                    body.Append(txtDeltaTablePath.Text);
                    body.Append("'\")");
                    body.Append(Environment.NewLine);

                }
                else
                {
                    body.Append(Environment.NewLine);
                    body.Append("df.write.saveAsTable(");
                    body.Append("\"");

                    if (txtDatabaseSave.Text.Length > 0)
                    {
                        body.Append(txtDatabaseSave.Text);
                        body.Append(".");
                    }

                    body.Append(lsvTables.SelectedItems[0].SubItems[1].Text);
                    body.Append("\"");
                    body.Append(")");
                    body.Append(Environment.NewLine);
                }



            }

            // write out file
            writeToTempFile(body.ToString());

        }

        private void writeToTempFile(string contents)
        {
            
            // get a temporary file
            string tempFile = Path.GetTempFileName();

            File.WriteAllText(tempFile, contents);

            Process p = new Process();
            ProcessStartInfo psi = new ProcessStartInfo("notepad.exe", tempFile);

            p.StartInfo = psi;
            p.Start();

        }

        private void BtnFilter_Click(object sender, EventArgs e)
        {
            string[] filters = txtFilter.Text.Split(new char[] { ',' });

            foreach (string s in filters)
            {
                foreach(ListViewItem l in lsvTables.Items)
                {
                    if (l.Text.ToLower() != s.Trim().ToLower() && l.Tag != "PIN")
                    {
                        l.Tag = "REMOVE";
                    }
                    else
                    {
                        l.Tag = "PIN";
                    }
                }
            }

            // clean-up
            foreach (ListViewItem l in lsvTables.Items)
            {
                if (l.Tag == "REMOVE")
                {
                    l.Remove();
                }
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            getTableListing();
            txtFilter.Text = "";
        }

        private void TxtFilter_TextChanged(object sender, EventArgs e)
        {
            if (txtFilter.Text.Length > 0)
            {
                btnFilter.Enabled = true;
            }
            else
            {
                btnFilter.Enabled = false;
            }

        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            lsvTables.SelectedItems[0].Remove();
        }

        private void ChkIncludeSaveAsTable_CheckedChanged(object sender, EventArgs e)
        {
            if (chkIncludeSaveAsTable.Checked)
            {
                chkDeltaTable.Enabled = true;
                lblDeltaName.Enabled = true;
                txtDeltaTablePath.Enabled = true;
                txtDatabaseSave.Enabled = true;
                lblDatabase.Enabled = true;
            }
            else
            {
                txtDatabaseSave.Enabled = false;
                lblDatabase.Enabled = false;
                chkDeltaTable.Enabled = false;
                lblDeltaName.Enabled = false;
                txtDeltaTablePath.Enabled = false;
            }
        }
    }
}
