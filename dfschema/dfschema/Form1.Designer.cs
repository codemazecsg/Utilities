namespace dfschema
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.grpLogon = new System.Windows.Forms.GroupBox();
            this.lblUserName = new System.Windows.Forms.Label();
            this.txtUserName = new System.Windows.Forms.TextBox();
            this.btnLogon = new System.Windows.Forms.Button();
            this.lblDatabaseName = new System.Windows.Forms.Label();
            this.txtDatabaseName = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.lblServername = new System.Windows.Forms.Label();
            this.txtServerName = new System.Windows.Forms.TextBox();
            this.grpTables = new System.Windows.Forms.GroupBox();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnFilter = new System.Windows.Forms.Button();
            this.txtFilter = new System.Windows.Forms.TextBox();
            this.grpOptions = new System.Windows.Forms.GroupBox();
            this.txtSeparator = new System.Windows.Forms.TextBox();
            this.lblSeparator = new System.Windows.Forms.Label();
            this.txtDateFormat = new System.Windows.Forms.TextBox();
            this.lblDateFormat = new System.Windows.Forms.Label();
            this.chkHeader = new System.Windows.Forms.CheckBox();
            this.lblFilePath = new System.Windows.Forms.Label();
            this.txtFilePath = new System.Windows.Forms.TextBox();
            this.grpImports = new System.Windows.Forms.GroupBox();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnAddImport = new System.Windows.Forms.Button();
            this.txtImport = new System.Windows.Forms.TextBox();
            this.lsbImports = new System.Windows.Forms.ListBox();
            this.lsvTables = new System.Windows.Forms.ListView();
            this.colSchema = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colTableName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnBuild = new System.Windows.Forms.Button();
            this.chkIncludeSaveAsTable = new System.Windows.Forms.CheckBox();
            this.lblDatabase = new System.Windows.Forms.Label();
            this.txtDatabaseSave = new System.Windows.Forms.TextBox();
            this.lblNullValue = new System.Windows.Forms.Label();
            this.txtNullValues = new System.Windows.Forms.TextBox();
            this.grpLogon.SuspendLayout();
            this.grpTables.SuspendLayout();
            this.grpOptions.SuspendLayout();
            this.grpImports.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpLogon
            // 
            this.grpLogon.Controls.Add(this.lblUserName);
            this.grpLogon.Controls.Add(this.txtUserName);
            this.grpLogon.Controls.Add(this.btnLogon);
            this.grpLogon.Controls.Add(this.lblDatabaseName);
            this.grpLogon.Controls.Add(this.txtDatabaseName);
            this.grpLogon.Controls.Add(this.lblPassword);
            this.grpLogon.Controls.Add(this.txtPassword);
            this.grpLogon.Controls.Add(this.lblServername);
            this.grpLogon.Controls.Add(this.txtServerName);
            this.grpLogon.Location = new System.Drawing.Point(12, 12);
            this.grpLogon.Name = "grpLogon";
            this.grpLogon.Size = new System.Drawing.Size(1002, 75);
            this.grpLogon.TabIndex = 0;
            this.grpLogon.TabStop = false;
            this.grpLogon.Text = "Logon Credentials";
            // 
            // lblUserName
            // 
            this.lblUserName.AutoSize = true;
            this.lblUserName.Location = new System.Drawing.Point(264, 18);
            this.lblUserName.Name = "lblUserName";
            this.lblUserName.Size = new System.Drawing.Size(58, 13);
            this.lblUserName.TabIndex = 8;
            this.lblUserName.Text = "Username";
            // 
            // txtUserName
            // 
            this.txtUserName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtUserName.Location = new System.Drawing.Point(267, 34);
            this.txtUserName.Name = "txtUserName";
            this.txtUserName.Size = new System.Drawing.Size(157, 22);
            this.txtUserName.TabIndex = 1;
            // 
            // btnLogon
            // 
            this.btnLogon.Cursor = System.Windows.Forms.Cursors.Default;
            this.btnLogon.Location = new System.Drawing.Point(881, 34);
            this.btnLogon.Name = "btnLogon";
            this.btnLogon.Size = new System.Drawing.Size(101, 22);
            this.btnLogon.TabIndex = 4;
            this.btnLogon.Text = "&Logon";
            this.btnLogon.UseVisualStyleBackColor = true;
            this.btnLogon.Click += new System.EventHandler(this.BtnLogon_Click);
            // 
            // lblDatabaseName
            // 
            this.lblDatabaseName.AutoSize = true;
            this.lblDatabaseName.Location = new System.Drawing.Point(644, 18);
            this.lblDatabaseName.Name = "lblDatabaseName";
            this.lblDatabaseName.Size = new System.Drawing.Size(87, 13);
            this.lblDatabaseName.TabIndex = 5;
            this.lblDatabaseName.Text = "Database Name";
            // 
            // txtDatabaseName
            // 
            this.txtDatabaseName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDatabaseName.Location = new System.Drawing.Point(647, 34);
            this.txtDatabaseName.Name = "txtDatabaseName";
            this.txtDatabaseName.Size = new System.Drawing.Size(216, 22);
            this.txtDatabaseName.TabIndex = 3;
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(439, 18);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(56, 13);
            this.lblPassword.TabIndex = 3;
            this.lblPassword.Text = "Password";
            // 
            // txtPassword
            // 
            this.txtPassword.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPassword.Location = new System.Drawing.Point(442, 34);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(180, 22);
            this.txtPassword.TabIndex = 2;
            // 
            // lblServername
            // 
            this.lblServername.AutoSize = true;
            this.lblServername.Location = new System.Drawing.Point(17, 18);
            this.lblServername.Name = "lblServername";
            this.lblServername.Size = new System.Drawing.Size(66, 13);
            this.lblServername.TabIndex = 1;
            this.lblServername.Text = "Servername";
            // 
            // txtServerName
            // 
            this.txtServerName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtServerName.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtServerName.Location = new System.Drawing.Point(20, 34);
            this.txtServerName.Name = "txtServerName";
            this.txtServerName.Size = new System.Drawing.Size(232, 22);
            this.txtServerName.TabIndex = 0;
            // 
            // grpTables
            // 
            this.grpTables.Controls.Add(this.btnRemove);
            this.grpTables.Controls.Add(this.btnReset);
            this.grpTables.Controls.Add(this.btnFilter);
            this.grpTables.Controls.Add(this.txtFilter);
            this.grpTables.Controls.Add(this.grpOptions);
            this.grpTables.Controls.Add(this.grpImports);
            this.grpTables.Controls.Add(this.lsvTables);
            this.grpTables.Enabled = false;
            this.grpTables.Location = new System.Drawing.Point(12, 93);
            this.grpTables.Name = "grpTables";
            this.grpTables.Size = new System.Drawing.Size(1002, 472);
            this.grpTables.TabIndex = 1;
            this.grpTables.TabStop = false;
            this.grpTables.Text = "Tables";
            // 
            // btnRemove
            // 
            this.btnRemove.Enabled = false;
            this.btnRemove.Location = new System.Drawing.Point(870, 22);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(63, 23);
            this.btnRemove.TabIndex = 6;
            this.btnRemove.Text = "Remove";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.BtnRemove_Click);
            // 
            // btnReset
            // 
            this.btnReset.Location = new System.Drawing.Point(939, 22);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(57, 23);
            this.btnReset.TabIndex = 5;
            this.btnReset.Text = "Reset";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.BtnReset_Click);
            // 
            // btnFilter
            // 
            this.btnFilter.Enabled = false;
            this.btnFilter.Location = new System.Drawing.Point(737, 22);
            this.btnFilter.Name = "btnFilter";
            this.btnFilter.Size = new System.Drawing.Size(101, 23);
            this.btnFilter.TabIndex = 4;
            this.btnFilter.Text = "Filter By Schema";
            this.btnFilter.UseVisualStyleBackColor = true;
            this.btnFilter.Click += new System.EventHandler(this.BtnFilter_Click);
            // 
            // txtFilter
            // 
            this.txtFilter.Location = new System.Drawing.Point(6, 23);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.Size = new System.Drawing.Size(725, 22);
            this.txtFilter.TabIndex = 3;
            this.txtFilter.TextChanged += new System.EventHandler(this.TxtFilter_TextChanged);
            // 
            // grpOptions
            // 
            this.grpOptions.Controls.Add(this.txtNullValues);
            this.grpOptions.Controls.Add(this.lblNullValue);
            this.grpOptions.Controls.Add(this.txtSeparator);
            this.grpOptions.Controls.Add(this.lblSeparator);
            this.grpOptions.Controls.Add(this.txtDateFormat);
            this.grpOptions.Controls.Add(this.lblDateFormat);
            this.grpOptions.Controls.Add(this.chkHeader);
            this.grpOptions.Controls.Add(this.lblFilePath);
            this.grpOptions.Controls.Add(this.txtFilePath);
            this.grpOptions.Cursor = System.Windows.Forms.Cursors.Default;
            this.grpOptions.Enabled = false;
            this.grpOptions.Location = new System.Drawing.Point(7, 388);
            this.grpOptions.Name = "grpOptions";
            this.grpOptions.Size = new System.Drawing.Size(989, 74);
            this.grpOptions.TabIndex = 2;
            this.grpOptions.TabStop = false;
            this.grpOptions.Text = "Options";
            // 
            // txtSeparator
            // 
            this.txtSeparator.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSeparator.Location = new System.Drawing.Point(423, 45);
            this.txtSeparator.Name = "txtSeparator";
            this.txtSeparator.Size = new System.Drawing.Size(50, 22);
            this.txtSeparator.TabIndex = 6;
            this.txtSeparator.Text = ",";
            // 
            // lblSeparator
            // 
            this.lblSeparator.AutoSize = true;
            this.lblSeparator.Location = new System.Drawing.Point(357, 50);
            this.lblSeparator.Name = "lblSeparator";
            this.lblSeparator.Size = new System.Drawing.Size(60, 13);
            this.lblSeparator.TabIndex = 5;
            this.lblSeparator.Text = "Separator:";
            // 
            // txtDateFormat
            // 
            this.txtDateFormat.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDateFormat.Location = new System.Drawing.Point(235, 46);
            this.txtDateFormat.Name = "txtDateFormat";
            this.txtDateFormat.Size = new System.Drawing.Size(100, 22);
            this.txtDateFormat.TabIndex = 4;
            this.txtDateFormat.Text = "MM/dd/yyyy";
            // 
            // lblDateFormat
            // 
            this.lblDateFormat.AutoSize = true;
            this.lblDateFormat.Location = new System.Drawing.Point(159, 50);
            this.lblDateFormat.Name = "lblDateFormat";
            this.lblDateFormat.Size = new System.Drawing.Size(70, 13);
            this.lblDateFormat.TabIndex = 3;
            this.lblDateFormat.Text = "DateFormat:";
            // 
            // chkHeader
            // 
            this.chkHeader.AutoSize = true;
            this.chkHeader.Location = new System.Drawing.Point(71, 49);
            this.chkHeader.Name = "chkHeader";
            this.chkHeader.Size = new System.Drawing.Size(63, 17);
            this.chkHeader.TabIndex = 2;
            this.chkHeader.Text = "Header";
            this.chkHeader.UseVisualStyleBackColor = true;
            // 
            // lblFilePath
            // 
            this.lblFilePath.AutoSize = true;
            this.lblFilePath.Location = new System.Drawing.Point(11, 20);
            this.lblFilePath.Name = "lblFilePath";
            this.lblFilePath.Size = new System.Drawing.Size(54, 13);
            this.lblFilePath.TabIndex = 1;
            this.lblFilePath.Text = "File Path:";
            // 
            // txtFilePath
            // 
            this.txtFilePath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtFilePath.Location = new System.Drawing.Point(71, 18);
            this.txtFilePath.Name = "txtFilePath";
            this.txtFilePath.Size = new System.Drawing.Size(904, 22);
            this.txtFilePath.TabIndex = 0;
            // 
            // grpImports
            // 
            this.grpImports.Controls.Add(this.btnDelete);
            this.grpImports.Controls.Add(this.btnAddImport);
            this.grpImports.Controls.Add(this.txtImport);
            this.grpImports.Controls.Add(this.lsbImports);
            this.grpImports.Enabled = false;
            this.grpImports.Location = new System.Drawing.Point(7, 261);
            this.grpImports.Name = "grpImports";
            this.grpImports.Size = new System.Drawing.Size(989, 120);
            this.grpImports.TabIndex = 1;
            this.grpImports.TabStop = false;
            this.grpImports.Text = "Imports";
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(889, 22);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(86, 23);
            this.btnDelete.TabIndex = 3;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.BtnDelete_Click);
            // 
            // btnAddImport
            // 
            this.btnAddImport.Location = new System.Drawing.Point(785, 85);
            this.btnAddImport.Name = "btnAddImport";
            this.btnAddImport.Size = new System.Drawing.Size(85, 23);
            this.btnAddImport.TabIndex = 2;
            this.btnAddImport.Text = "Add";
            this.btnAddImport.UseVisualStyleBackColor = true;
            this.btnAddImport.Click += new System.EventHandler(this.BtnAddImport_Click);
            // 
            // txtImport
            // 
            this.txtImport.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtImport.Location = new System.Drawing.Point(13, 85);
            this.txtImport.Name = "txtImport";
            this.txtImport.Size = new System.Drawing.Size(756, 22);
            this.txtImport.TabIndex = 1;
            this.txtImport.TextChanged += new System.EventHandler(this.TxtImport_TextChanged);
            // 
            // lsbImports
            // 
            this.lsbImports.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lsbImports.FormattingEnabled = true;
            this.lsbImports.Location = new System.Drawing.Point(13, 22);
            this.lsbImports.Name = "lsbImports";
            this.lsbImports.Size = new System.Drawing.Size(857, 41);
            this.lsbImports.TabIndex = 0;
            this.lsbImports.SelectedIndexChanged += new System.EventHandler(this.LsbImports_SelectedIndexChanged);
            // 
            // lsvTables
            // 
            this.lsvTables.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lsvTables.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colSchema,
            this.colTableName});
            this.lsvTables.FullRowSelect = true;
            this.lsvTables.GridLines = true;
            this.lsvTables.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lsvTables.HideSelection = false;
            this.lsvTables.Location = new System.Drawing.Point(6, 51);
            this.lsvTables.MultiSelect = false;
            this.lsvTables.Name = "lsvTables";
            this.lsvTables.Size = new System.Drawing.Size(990, 204);
            this.lsvTables.TabIndex = 0;
            this.lsvTables.UseCompatibleStateImageBehavior = false;
            this.lsvTables.View = System.Windows.Forms.View.Details;
            this.lsvTables.SelectedIndexChanged += new System.EventHandler(this.LsvTables_SelectedIndexChanged);
            this.lsvTables.DoubleClick += new System.EventHandler(this.LsvTables_DoubleClick);
            // 
            // colSchema
            // 
            this.colSchema.Text = "Schema";
            this.colSchema.Width = 102;
            // 
            // colTableName
            // 
            this.colTableName.Text = "Tablename";
            this.colTableName.Width = 875;
            // 
            // btnBuild
            // 
            this.btnBuild.Enabled = false;
            this.btnBuild.Location = new System.Drawing.Point(933, 582);
            this.btnBuild.Name = "btnBuild";
            this.btnBuild.Size = new System.Drawing.Size(75, 23);
            this.btnBuild.TabIndex = 2;
            this.btnBuild.Text = "&Build";
            this.btnBuild.UseVisualStyleBackColor = true;
            this.btnBuild.Click += new System.EventHandler(this.BtnBuild_Click);
            // 
            // chkIncludeSaveAsTable
            // 
            this.chkIncludeSaveAsTable.AutoSize = true;
            this.chkIncludeSaveAsTable.Location = new System.Drawing.Point(13, 582);
            this.chkIncludeSaveAsTable.Name = "chkIncludeSaveAsTable";
            this.chkIncludeSaveAsTable.Size = new System.Drawing.Size(128, 17);
            this.chkIncludeSaveAsTable.TabIndex = 3;
            this.chkIncludeSaveAsTable.Text = "Include saveAsTable";
            this.chkIncludeSaveAsTable.UseVisualStyleBackColor = true;
            // 
            // lblDatabase
            // 
            this.lblDatabase.AutoSize = true;
            this.lblDatabase.Location = new System.Drawing.Point(157, 583);
            this.lblDatabase.Name = "lblDatabase";
            this.lblDatabase.Size = new System.Drawing.Size(61, 13);
            this.lblDatabase.TabIndex = 4;
            this.lblDatabase.Text = "Database: ";
            // 
            // txtDatabaseSave
            // 
            this.txtDatabaseSave.Location = new System.Drawing.Point(224, 580);
            this.txtDatabaseSave.Name = "txtDatabaseSave";
            this.txtDatabaseSave.Size = new System.Drawing.Size(180, 22);
            this.txtDatabaseSave.TabIndex = 5;
            // 
            // lblNullValue
            // 
            this.lblNullValue.AutoSize = true;
            this.lblNullValue.Location = new System.Drawing.Point(506, 50);
            this.lblNullValue.Name = "lblNullValue";
            this.lblNullValue.Size = new System.Drawing.Size(60, 13);
            this.lblNullValue.TabIndex = 6;
            this.lblNullValue.Text = "null value:";
            // 
            // txtNullValues
            // 
            this.txtNullValues.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtNullValues.Location = new System.Drawing.Point(572, 45);
            this.txtNullValues.Name = "txtNullValues";
            this.txtNullValues.Size = new System.Drawing.Size(100, 22);
            this.txtNullValues.TabIndex = 6;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(1024, 617);
            this.Controls.Add(this.txtDatabaseSave);
            this.Controls.Add(this.lblDatabase);
            this.Controls.Add(this.chkIncludeSaveAsTable);
            this.Controls.Add(this.btnBuild);
            this.Controls.Add(this.grpTables);
            this.Controls.Add(this.grpLogon);
            this.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "Form1";
            this.Text = "dfSchema";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.grpLogon.ResumeLayout(false);
            this.grpLogon.PerformLayout();
            this.grpTables.ResumeLayout(false);
            this.grpTables.PerformLayout();
            this.grpOptions.ResumeLayout(false);
            this.grpOptions.PerformLayout();
            this.grpImports.ResumeLayout(false);
            this.grpImports.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox grpLogon;
        private System.Windows.Forms.Button btnLogon;
        private System.Windows.Forms.Label lblDatabaseName;
        private System.Windows.Forms.TextBox txtDatabaseName;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label lblServername;
        private System.Windows.Forms.TextBox txtServerName;
        private System.Windows.Forms.GroupBox grpTables;
        private System.Windows.Forms.Label lblUserName;
        private System.Windows.Forms.TextBox txtUserName;
        private System.Windows.Forms.ListView lsvTables;
        private System.Windows.Forms.ColumnHeader colSchema;
        private System.Windows.Forms.ColumnHeader colTableName;
        private System.Windows.Forms.GroupBox grpImports;
        private System.Windows.Forms.Button btnAddImport;
        private System.Windows.Forms.TextBox txtImport;
        private System.Windows.Forms.ListBox lsbImports;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.GroupBox grpOptions;
        private System.Windows.Forms.TextBox txtSeparator;
        private System.Windows.Forms.Label lblSeparator;
        private System.Windows.Forms.TextBox txtDateFormat;
        private System.Windows.Forms.Label lblDateFormat;
        private System.Windows.Forms.CheckBox chkHeader;
        private System.Windows.Forms.Label lblFilePath;
        private System.Windows.Forms.TextBox txtFilePath;
        private System.Windows.Forms.Button btnBuild;
        private System.Windows.Forms.CheckBox chkIncludeSaveAsTable;
        private System.Windows.Forms.Label lblDatabase;
        private System.Windows.Forms.TextBox txtDatabaseSave;
        private System.Windows.Forms.Button btnFilter;
        private System.Windows.Forms.TextBox txtFilter;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.TextBox txtNullValues;
        private System.Windows.Forms.Label lblNullValue;
    }
}

