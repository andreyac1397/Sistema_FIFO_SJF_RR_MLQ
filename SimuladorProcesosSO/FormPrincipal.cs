using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SimuladorProcesosSO_LOGICA;

namespace SimuladorProcesosSO
{
    public class FormPrincipal : Form
    {
        // ---- lógica ----
        private readonly Simulador _sim = new Simulador();
        private readonly GestorArchivos _files = new GestorArchivos();
        private ResultadoSimulacion _ultimo;
        private List<ResultadoSimulacion> _historialEjecuciones = new List<ResultadoSimulacion>();

        // ---- header ----
        private ComboBox cboAlgoritmo;
        private Button btnDemo;
        private Button btnCargar;
        private Button btnEjecutar;
        private Button btnExportarCSV;
        private Button btnExportarGantt;
        private Button btnComparativo;
        private Button btnTiquetes;

        // ---- panel MLQ ----
        private Panel panelMLQ;
        private ComboBox cboCola1;
        private ComboBox cboCola2;
        private ComboBox cboCola3;
        private NumericUpDown nudQ1;
        private NumericUpDown nudQ2;
        private NumericUpDown nudQ3;

        // ---- cuerpo ----
        private DataGridView dgvProcesos;
        private DataGridView dgvResultados;
        private Panel pnlGanttVisual;
        private ProgressBar pbAvance;
        private Label lblEstadoSimulacion;
        private ListView lvTiquetes;

        // ---- métricas ----
        private Label lblTotalProcesos;

        // ---- configuración tipos de cliente ----
        private Dictionary<string, ConfiguracionCliente> _configClientes = new Dictionary<string, ConfiguracionCliente>
        {
            {"VIP", new ConfiguracionCliente { Prioridad = 1, MaxGestiones = 10, ColorTiquete = Color.Gold }},
            {"Adulto Mayor", new ConfiguracionCliente { Prioridad = 1, MaxGestiones = 5, ColorTiquete = Color.LightBlue }},
            {"Embarazada", new ConfiguracionCliente { Prioridad = 1, MaxGestiones = 5, ColorTiquete = Color.LightPink }},
            {"Regular", new ConfiguracionCliente { Prioridad = 2, MaxGestiones = 5, ColorTiquete = Color.LightGray }},
            {"Foraneo", new ConfiguracionCliente { Prioridad = 3, MaxGestiones = 3, ColorTiquete = Color.LightGreen }}
        };

        private int _numeroTiquete = 1;

        public FormPrincipal()
        {
            Text = "Simulador de Planificación - Sistema Bancario con Tiquetes";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1200, 750);
            BackColor = Color.FromArgb(236, 239, 244);

            BuildUI();
        }

        private void BuildUI()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            // ================= HEADER =================
            var header = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10, 10, 10, 5)
            };
            root.Controls.Add(header, 0, 0);

            var lblTitulo = new Label
            {
                Text = "Simulador de Planificación - Sistema Bancario",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Dock = DockStyle.Left
            };

            var flowTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false
            };

            var lblAlg = new Label
            {
                Text = "Algoritmo:",
                AutoSize = true,
                Margin = new Padding(5, 8, 0, 0),
                Font = new Font("Segoe UI", 9)
            };

            cboAlgoritmo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 140,
                Font = new Font("Segoe UI", 9),
                Margin = new Padding(3, 5, 0, 0),
                Enabled = false
            };
            cboAlgoritmo.Items.Add("MLQ");
            cboAlgoritmo.SelectedIndex = 0;

            btnTiquetes = MakeTopButton("Generar Tiquete");
            btnTiquetes.Click += BtnTiquetes_Click;

            btnDemo = MakeTopButton("Demo");
            btnDemo.Click += BtnDemo_Click;

            btnCargar = MakeTopButton("Cargar TXT/CSV");
            btnCargar.Click += BtnCargar_Click;

            btnEjecutar = MakeTopButton("Ejecutar");
            btnEjecutar.Click += BtnEjecutar_Click;

            btnComparativo = MakeTopButton("Reporte Comparativo");
            btnComparativo.Click += BtnComparativo_Click;

            btnExportarCSV = MakeTopButton("Exportar CSV");
            btnExportarCSV.Click += BtnExportarCSV_Click;

            btnExportarGantt = MakeTopButton("Exportar Gantt");
            btnExportarGantt.Click += BtnExportarGantt_Click;

            flowTop.Controls.Add(lblAlg);
            flowTop.Controls.Add(cboAlgoritmo);
            flowTop.Controls.Add(btnTiquetes);
            flowTop.Controls.Add(btnDemo);
            flowTop.Controls.Add(btnCargar);
            flowTop.Controls.Add(btnEjecutar);
            flowTop.Controls.Add(btnComparativo);
            flowTop.Controls.Add(btnExportarCSV);
            flowTop.Controls.Add(btnExportarGantt);

            header.Controls.Add(flowTop);
            header.Controls.Add(lblTitulo);

            // ================= PANEL MLQ =================
            panelMLQ = BuildPanelMLQ();
            panelMLQ.Visible = true;
            root.Controls.Add(panelMLQ, 0, 1);

            // ================= BARRA DE PROGRESO =================
            var panelProgreso = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10, 5, 10, 5)
            };
            root.Controls.Add(panelProgreso, 0, 2);

            lblEstadoSimulacion = new Label
            {
                Text = "Estado: Esperando ejecución",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9),
                Height = 18
            };
            panelProgreso.Controls.Add(lblEstadoSimulacion);

            pbAvance = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Style = ProgressBarStyle.Continuous,
                Height = 12
            };
            panelProgreso.Controls.Add(pbAvance);

            // ================= BODY =================
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(6)
            };
            root.Controls.Add(body, 0, 3);

            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 75));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

            // ========== COLUMNA 1: PROCESOS ==========
            var panelIzq = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle
            };
            body.Controls.Add(panelIzq, 0, 0);
            body.SetRowSpan(panelIzq, 2);

            var lblProc = new Label
            {
                Text = "Clientes / Procesos",
                Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 22
            };
            

            dgvProcesos = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dgvProcesos.EnableHeadersVisualStyles = false;
            dgvProcesos.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
            dgvProcesos.Columns.Add("ID", "ID");
            dgvProcesos.Columns.Add("Llegada", "Llegada");
            dgvProcesos.Columns.Add("Rafaga", "Ráfaga");
            dgvProcesos.Columns.Add("Prioridad", "Prior.");
            dgvProcesos.Columns.Add("TipoCliente", "Tipo");
            panelIzq.Controls.Add(dgvProcesos);

            lblTotalProcesos = new Label
            {
                Text = "Clientes: 0",
                Dock = DockStyle.Bottom,
                Height = 18,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.DimGray
            };
            var lblFormato = new Label
            {
                Text = "Formato: ID,Llegada,Ráfaga,Prioridad,Tipo",
                Dock = DockStyle.Bottom,
                Height = 18,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.DimGray
            };
            panelIzq.Controls.Add(lblTotalProcesos);
            panelIzq.Controls.Add(lblFormato);

            // ========== COLUMNA 2: TIQUETES ==========
            var panelTiquetes = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle
            };
            body.Controls.Add(panelTiquetes, 1, 0);
            body.SetRowSpan(panelTiquetes, 2);

            var lblTiq = new Label
            {
                Text = "Sistema de Tiquetes",
                Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 22
            };
            panelTiquetes.Controls.Add(lblTiq);

            lvTiquetes = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.White
            };
            lvTiquetes.Columns.Add("Tiquete", 60);
            lvTiquetes.Columns.Add("Tipo", 80);
            lvTiquetes.Columns.Add("Gestiones", 60);
            panelTiquetes.Controls.Add(lvTiquetes);

            // ========== COLUMNA 3 ARRIBA: GANTT ==========
            var panelGantt = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle
            };
            body.Controls.Add(panelGantt, 2, 0);

            pnlGanttVisual = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };
            panelGantt.Controls.Add(pnlGanttVisual);

            // ========== COLUMNA 3 ABAJO: RESULTADOS ==========
            var panelResultados = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle
            };
            body.Controls.Add(panelResultados, 2, 1);

            dgvResultados = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
            };
            dgvResultados.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgvResultados.EnableHeadersVisualStyles = false;
            dgvResultados.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
            dgvResultados.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            dgvResultados.ColumnHeadersHeight = 22;

            panelResultados.Controls.Add(dgvResultados);
        }

        private Panel BuildPanelMLQ()
        {
            var p = new Panel
            {
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 4, 10, 4)
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false
            };

            ComboBox MakeCombo()
            {
                var c = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Width = 110,
                    Font = new Font("Segoe UI", 8)
                };
                c.Items.AddRange(new object[] { "FIFO", "SJF", "Round Robin" });
                c.SelectedIndex = 0;
                return c;
            }

            NumericUpDown MakeNUD()
            {
                return new NumericUpDown
                {
                    Minimum = 1,
                    Maximum = 50,
                    Value = 4,
                    Width = 45,
                    Font = new Font("Segoe UI", 8),
                    Enabled = false
                };
            }

            flow.Controls.Add(new Label { Text = "Cola 1 (VIP/Prioritarios):", Margin = new Padding(2, 9, 2, 0), AutoSize = true });
            cboCola1 = MakeCombo();
            flow.Controls.Add(cboCola1);
            flow.Controls.Add(new Label { Text = "Q:", Margin = new Padding(2, 9, 2, 0), AutoSize = true });
            nudQ1 = MakeNUD();
            flow.Controls.Add(nudQ1);
            cboCola1.SelectedIndexChanged += (s, e) => nudQ1.Enabled = (cboCola1.SelectedItem.ToString() == "Round Robin");

            flow.Controls.Add(new Label { Text = "   Cola 2 (Regular):", Margin = new Padding(10, 9, 2, 0), AutoSize = true });
            cboCola2 = MakeCombo();
            flow.Controls.Add(cboCola2);
            flow.Controls.Add(new Label { Text = "Q:", Margin = new Padding(2, 9, 2, 0), AutoSize = true });
            nudQ2 = MakeNUD();
            flow.Controls.Add(nudQ2);
            cboCola2.SelectedIndexChanged += (s, e) => nudQ2.Enabled = (cboCola2.SelectedItem.ToString() == "Round Robin");

            flow.Controls.Add(new Label { Text = "   Cola 3 (Foráneos):", Margin = new Padding(10, 9, 2, 0), AutoSize = true });
            cboCola3 = MakeCombo();
            flow.Controls.Add(cboCola3);
            flow.Controls.Add(new Label { Text = "Q:", Margin = new Padding(2, 9, 2, 0), AutoSize = true });
            nudQ3 = MakeNUD();
            flow.Controls.Add(nudQ3);
            cboCola3.SelectedIndexChanged += (s, e) => nudQ3.Enabled = (cboCola3.SelectedItem.ToString() == "Round Robin");

            p.Controls.Add(flow);
            return p;
        }

        // ======================= EVENTOS =======================
        private void BtnTiquetes_Click(object sender, EventArgs e)
        {
            using (var frmTiq = new FormGenerarTiquete(_configClientes))
            {
                if (frmTiq.ShowDialog() == DialogResult.OK)
                {
                    var config = _configClientes[frmTiq.TipoSeleccionado];
                    var item = new ListViewItem(_numeroTiquete.ToString("D4"));
                    item.SubItems.Add(frmTiq.TipoSeleccionado);
                    item.SubItems.Add(frmTiq.NumGestiones.ToString());
                    item.BackColor = config.ColorTiquete;
                    item.Tag = new
                    {
                        Numero = _numeroTiquete,
                        Tipo = frmTiq.TipoSeleccionado,
                        Gestiones = frmTiq.NumGestiones,
                        Llegada = DateTime.Now
                    };
                    lvTiquetes.Items.Add(item);

                    // Agregar a la grilla de procesos
                    int rafaga = frmTiq.NumGestiones * 2;
                    int llegada = dgvProcesos.Rows.Count;
                    dgvProcesos.Rows.Add(_numeroTiquete, llegada, rafaga, config.Prioridad, frmTiq.TipoSeleccionado);

                    _numeroTiquete++;
                    lblTotalProcesos.Text = $"Clientes: {dgvProcesos.Rows.Count}";
                    lblEstadoSimulacion.Text = $"Estado: Tiquete #{_numeroTiquete - 1} generado";
                }
            }
        }

        private void BtnDemo_Click(object sender, EventArgs e)
        {
            dgvProcesos.Rows.Clear();
            lvTiquetes.Items.Clear();
            _numeroTiquete = 1;

            var demo = new List<(string tipo, int gestiones)>
            {
                ("VIP", 3),
                ("VIP", 2),
                ("Adulto Mayor", 2),
                ("Regular", 3),
                ("Regular", 2),
                ("Embarazada", 1),
                ("Foraneo", 2),
                ("Regular", 4)
            };

            int llegada = 0;
            foreach (var (tipo, gestiones) in demo)
            {
                var config = _configClientes[tipo];
                int rafaga = gestiones * 2;

                dgvProcesos.Rows.Add(_numeroTiquete, llegada, rafaga, config.Prioridad, tipo);

                var item = new ListViewItem(_numeroTiquete.ToString("D4"));
                item.SubItems.Add(tipo);
                item.SubItems.Add(gestiones.ToString());
                item.BackColor = config.ColorTiquete;
                lvTiquetes.Items.Add(item);

                _numeroTiquete++;
                llegada += 1;
            }

            lblTotalProcesos.Text = $"Clientes: {demo.Count}";
            lblEstadoSimulacion.Text = "Estado: Datos de demostración cargados";
        }

        private void BtnCargar_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "TXT/CSV|*.txt;*.csv|Todos|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var procesos = _files.CargarProcesos(ofd.FileName, true, null);
                        dgvProcesos.Rows.Clear();
                        lvTiquetes.Items.Clear();

                        foreach (var p in procesos)
                        {
                            dgvProcesos.Rows.Add(p.ID, p.TiempoLlegada, p.Rafaga, p.Prioridad, p.TipoCliente);

                            if (_configClientes.ContainsKey(p.TipoCliente))
                            {
                                var config = _configClientes[p.TipoCliente];
                                var item = new ListViewItem(p.ID.ToString("D4"));
                                item.SubItems.Add(p.TipoCliente);
                                item.SubItems.Add((p.Rafaga / 2).ToString());
                                item.BackColor = config.ColorTiquete;
                                lvTiquetes.Items.Add(item);
                            }
                        }

                        lblTotalProcesos.Text = $"Clientes: {procesos.Count}";
                        lblEstadoSimulacion.Text = $"Estado: Archivo cargado - {procesos.Count} clientes";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al cargar archivo: " + ex.Message);
                    }
                }
            }
        }

        private void BtnEjecutar_Click(object sender, EventArgs e)
        {
            try
            {
                var lista = LeerProcesosDesdeGrid();
                if (lista.Count == 0)
                {
                    MessageBox.Show("Primero cargue clientes o use Demo.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                lblEstadoSimulacion.Text = "Estado: Ejecutando simulación...";
                pbAvance.Value = 0;
                pbAvance.Maximum = 100;
                Application.DoEvents();

                var cfgMLQ = new Dictionary<int, string>();
                string c1 = cboCola1.SelectedItem.ToString();
                string c2 = cboCola2.SelectedItem.ToString();
                string c3 = cboCola3.SelectedItem.ToString();

                if (c1 == "Round Robin") c1 = $"RR:{(int)nudQ1.Value}";
                if (c2 == "Round Robin") c2 = $"RR:{(int)nudQ2.Value}";
                if (c3 == "Round Robin") c3 = $"RR:{(int)nudQ3.Value}";

                cfgMLQ[1] = c1;
                cfgMLQ[2] = c2;
                cfgMLQ[3] = c3;

                _ultimo = _sim.Ejecutar(TipoPlanificador.MLQ, lista, null, cfgMLQ);

                _historialEjecuciones.Add(_ultimo);

                pbAvance.Value = 50;
                Application.DoEvents();

                dgvResultados.DataSource = null;
                dgvResultados.DataSource = _ultimo.ResultadosPorProceso;

                // Personalizar encabezados después de cargar los datos
                if (dgvResultados.Columns.Count > 0)
                {
                    // Intentar renombrar columnas comunes
                    foreach (DataGridViewColumn col in dgvResultados.Columns)
                    {
                        string nombreOriginal = col.Name;

                        if (nombreOriginal.Contains("ID") || nombreOriginal.Contains("Proceso"))
                            col.HeaderText = "ID";
                        else if (nombreOriginal.Contains("Llegada"))
                            col.HeaderText = "Llegada";
                        else if (nombreOriginal.Contains("Rafaga") || nombreOriginal.Contains("Ráfaga"))
                            col.HeaderText = "Ráfaga";
                        else if (nombreOriginal.Contains("Inicio"))
                            col.HeaderText = "Inicio";
                        else if (nombreOriginal.Contains("Fin"))
                            col.HeaderText = "Fin";
                        else if (nombreOriginal.Contains("Espera"))
                            col.HeaderText = "Espera";
                        else if (nombreOriginal.Contains("Retorno"))
                            col.HeaderText = "Retorno";
                        else if (nombreOriginal.Contains("Tipo") || nombreOriginal.Contains("Cliente"))
                            col.HeaderText = "Tipo Cliente";
                        else if (nombreOriginal.Contains("Prioridad"))
                            col.HeaderText = "Prioridad";
                    }
                }

                pbAvance.Value = 75;
                Application.DoEvents();

                DibujarGantt(_ultimo.Gantt);

                pbAvance.Value = 100;
                lblEstadoSimulacion.Text = $"Estado: Simulación completada - {lista.Count} clientes procesados";

                MessageBox.Show($"Simulación completada exitosamente.\n\n" +
                    $"Clientes procesados: {lista.Count}\n" +
                    $"Tiempo total: {_ultimo.Estadisticas.TiempoTotalSimulacion}\n" +
                    $"Espera promedio: {_ultimo.Estadisticas.PromedioEspera:0.##}\n" +
                    $"Retorno promedio: {_ultimo.Estadisticas.PromedioRetorno:0.##}\n" +
                    $"Uso de CPU: {_ultimo.Estadisticas.UtilizacionCPU:P1}",
                    "Simulación Completada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                pbAvance.Value = 0;
                lblEstadoSimulacion.Text = "Estado: Error en la simulación";
                MessageBox.Show("Error al ejecutar simulación: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnComparativo_Click(object sender, EventArgs e)
        {
            if (_historialEjecuciones.Count == 0)
            {
                MessageBox.Show("No hay ejecuciones para comparar. Ejecute primero algunas simulaciones.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var frmComp = new FormComparativo(_historialEjecuciones);
            frmComp.ShowDialog();
        }

        private void BtnExportarCSV_Click(object sender, EventArgs e)
        {
            if (_ultimo == null || _ultimo.ResultadosPorProceso == null || _ultimo.ResultadosPorProceso.Count == 0)
            {
                MessageBox.Show("No hay resultados para exportar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV|*.csv";
                sfd.FileName = $"resultados_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _files.GuardarResultadosCsv(sfd.FileName, _ultimo.ResultadosPorProceso, _ultimo.Estadisticas);
                        lblEstadoSimulacion.Text = $"Estado: CSV exportado exitosamente";
                        MessageBox.Show("CSV guardado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al guardar CSV: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnExportarGantt_Click(object sender, EventArgs e)
        {
            if (_ultimo == null || _ultimo.Gantt == null || _ultimo.Gantt.Count == 0)
            {
                MessageBox.Show("No hay diagrama de Gantt para exportar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "TXT|*.txt";
                sfd.FileName = $"gantt_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Generar texto del Gantt a partir de la lista de tramos
                        var ganttTexto = new System.Text.StringBuilder();
                        ganttTexto.AppendLine("DIAGRAMA DE GANTT");
                        ganttTexto.AppendLine("=================\n");

                        foreach (var tramo in _ultimo.Gantt)
                        {
                            ganttTexto.AppendLine($"Proceso {tramo.ProcesoID}: Inicio={tramo.Inicio}, Fin={tramo.Fin}");
                        }

                        System.IO.File.WriteAllText(sfd.FileName, ganttTexto.ToString());
                        lblEstadoSimulacion.Text = $"Estado: Gantt exportado exitosamente";
                        MessageBox.Show("Diagrama de Gantt guardado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al guardar Gantt: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // ================== HELPERS ==================
        private List<Proceso> LeerProcesosDesdeGrid()
        {
            var list = new List<Proceso>();
            foreach (DataGridViewRow row in dgvProcesos.Rows)
            {
                if (row.IsNewRow) continue;

                int id = Convert.ToInt32(row.Cells["ID"].Value);
                int llegada = Convert.ToInt32(row.Cells["Llegada"].Value);
                int rafaga = Convert.ToInt32(row.Cells["Rafaga"].Value);
                int prioridad = Convert.ToInt32(row.Cells["Prioridad"].Value);
                string tipo = row.Cells["TipoCliente"].Value?.ToString() ?? "Regular";

                list.Add(new Proceso(id, llegada, rafaga, prioridad, tipo));
            }

            var repetidos = list.GroupBy(p => p.ID).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (repetidos.Count > 0)
                throw new Exception("IDs repetidos en procesos: " + string.Join(", ", repetidos));

            return list;
        }

        private Button MakeTopButton(string text)
        {
            return new Button
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(8, 4, 0, 0),
                FlatStyle = FlatStyle.Standard,
                Height = 28
            };
        }

        // ================== GANTT GRÁFICO ==================
        private void DibujarGantt(List<PlanificadorBase.TramoGantt> tramos)
        {
            pnlGanttVisual.Controls.Clear();

            if (tramos == null || tramos.Count == 0)
            {
                var lblVacio = new Label
                {
                    Text = "No hay datos para mostrar",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Color.Gray
                };
                pnlGanttVisual.Controls.Add(lblVacio);
                return;
            }

            int maxFin = tramos.Max(t => t.Fin);
            int escalaX = Math.Max(30, Math.Min(60, pnlGanttVisual.Width / (maxFin + 2)));
            int alturaBloque = 35;
            int margenSuperior = 30;
            int margenIzquierdo = 50;

            var panelDibujo = new Panel
            {
                Width = (maxFin + 2) * escalaX + margenIzquierdo + 20,
                Height = tramos.Count * (alturaBloque + 8) + margenSuperior + 40,
                BackColor = Color.White
            };

            panelDibujo.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.Clear(Color.White);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Eje de tiempo
                for (int t = 0; t <= maxFin; t++)
                {
                    int x = margenIzquierdo + t * escalaX;
                    g.DrawLine(Pens.LightGray, x, margenSuperior, x, panelDibujo.Height - 20);

                    // Centrar el número sobre la línea
                    string numStr = t.ToString();
                    var medidaNum = g.MeasureString(numStr, new Font("Segoe UI", 8));
                    g.DrawString(numStr, new Font("Segoe UI", 8), Brushes.Black,
                        x - (medidaNum.Width / 2), margenSuperior - 20);
                }

                var colores = new Dictionary<int, Color>();
                var random = new Random(42);

                for (int i = 0; i < tramos.Count; i++)
                {
                    var t = tramos[i];

                    if (!colores.ContainsKey(t.ProcesoID))
                    {
                        colores[t.ProcesoID] = Color.FromArgb(
                            150 + random.Next(105),
                            150 + random.Next(105),
                            150 + random.Next(105)
                        );
                    }

                    Rectangle rect = new Rectangle(
                        margenIzquierdo + t.Inicio * escalaX,
                        margenSuperior + i * (alturaBloque + 8),
                        (t.Fin - t.Inicio) * escalaX,
                        alturaBloque
                    );

                    // Dibujar bloque con gradiente
                    using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        rect, colores[t.ProcesoID],
                        Color.FromArgb(colores[t.ProcesoID].R - 30,
                                      colores[t.ProcesoID].G - 30,
                                      colores[t.ProcesoID].B - 30),
                        45f))
                    {
                        g.FillRectangle(brush, rect);
                    }

                    g.DrawRectangle(new Pen(Color.Black, 2), rect);

                    // Texto del proceso
                    string texto = $"P{t.ProcesoID}";
                    var medida = g.MeasureString(texto, new Font("Segoe UI", 9, FontStyle.Bold));
                    g.DrawString(texto,
                        new Font("Segoe UI", 9, FontStyle.Bold),
                        Brushes.White,
                        rect.X + (rect.Width - medida.Width) / 2,
                        rect.Y + (rect.Height - medida.Height) / 2);

                    // Tiempos - centrados bajo los bordes del bloque
                    string inicioStr = t.Inicio.ToString();
                    string finStr = t.Fin.ToString();
                    var medidaInicio = g.MeasureString(inicioStr, new Font("Segoe UI", 7));
                    var medidaFin = g.MeasureString(finStr, new Font("Segoe UI", 7));

                    g.DrawString(inicioStr, new Font("Segoe UI", 7), Brushes.Black,
                        rect.X - (medidaInicio.Width / 2), rect.Y + alturaBloque + 2);
                    g.DrawString(finStr, new Font("Segoe UI", 7), Brushes.Black,
                        rect.Right - (medidaFin.Width / 2), rect.Y + alturaBloque + 2);
                }
            };

            pnlGanttVisual.Controls.Add(panelDibujo);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // FormPrincipal
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "FormPrincipal";
            this.Load += new System.EventHandler(this.FormPrincipal_Load);
            this.ResumeLayout(false);

        }

        private void FormPrincipal_Load(object sender, EventArgs e)
        {

        }
    }

    // ======================= CLASES AUXILIARES =======================
    public class ConfiguracionCliente
    {
        public int Prioridad { get; set; }
        public int MaxGestiones { get; set; }
        public Color ColorTiquete { get; set; }
    }

    // ======================= FORM GENERAR TIQUETE =======================
    public class FormGenerarTiquete : Form
    {
        public string TipoSeleccionado { get; private set; }
        public int NumGestiones { get; private set; }

        private ComboBox cboTipo;
        private NumericUpDown nudGestiones;
        private TextBox txtDescripcion;

        public FormGenerarTiquete(Dictionary<string, ConfiguracionCliente> configuraciones)
        {
            Text = "Generar Tiquete de Atención";
            Size = new Size(450, 380);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            var pnlPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(20)
            };
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            Controls.Add(pnlPrincipal);

            // Título
            var lblTitulo = new Label
            {
                Text = "Sistema de Emisión de Tiquetes",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(0, 122, 204)
            };
            pnlPrincipal.Controls.Add(lblTitulo, 0, 0);

            // Tipo de cliente
            var pnlTipo = new Panel { Dock = DockStyle.Fill };
            var lblTipo = new Label
            {
                Text = "Tipo de Cliente:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 5)
            };
            cboTipo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                Width = 300,
                Location = new Point(0, 28)
            };
            foreach (var tipo in configuraciones.Keys)
            {
                cboTipo.Items.Add(tipo);
            }
            cboTipo.SelectedIndex = 0;
            pnlTipo.Controls.AddRange(new Control[] { lblTipo, cboTipo });
            pnlPrincipal.Controls.Add(pnlTipo, 0, 1);

            // Número de gestiones
            var pnlGestiones = new Panel { Dock = DockStyle.Fill };
            var lblGestiones = new Label
            {
                Text = "Número de Gestiones:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 5)
            };
            nudGestiones = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 10,
                Value = 1,
                Font = new Font("Segoe UI", 10),
                Width = 100,
                Location = new Point(0, 28)
            };
            var lblInfoGest = new Label
            {
                Text = "Máximo según tipo de cliente",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(110, 32)
            };
            pnlGestiones.Controls.AddRange(new Control[] { lblGestiones, nudGestiones, lblInfoGest });
            pnlPrincipal.Controls.Add(pnlGestiones, 0, 2);

            // Descripción
            var pnlDesc = new Panel { Dock = DockStyle.Fill };
            var lblDesc = new Label
            {
                Text = "Servicios Requeridos (Opcional):",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 5)
            };
            txtDescripcion = new TextBox
            {
                Multiline = true,
                Font = new Font("Segoe UI", 9),
                Width = 400,
                Height = 80,
                Location = new Point(0, 28)
            };
            pnlDesc.Controls.AddRange(new Control[] { lblDesc, txtDescripcion });
            pnlPrincipal.Controls.Add(pnlDesc, 0, 3);

            // Validar gestiones según tipo
            cboTipo.SelectedIndexChanged += (s, e) =>
            {
                var tipo = cboTipo.SelectedItem.ToString();
                nudGestiones.Maximum = configuraciones[tipo].MaxGestiones;
                if (nudGestiones.Value > nudGestiones.Maximum)
                    nudGestiones.Value = nudGestiones.Maximum;
            };

            // Botones
            var pnlBotones = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            var btnAceptar = new Button
            {
                Text = "Generar Tiquete",
                Size = new Size(130, 35),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnAceptar.Click += (s, e) =>
            {
                TipoSeleccionado = cboTipo.SelectedItem.ToString();
                NumGestiones = (int)nudGestiones.Value;
                DialogResult = DialogResult.OK;
                Close();
            };

            var btnCancelar = new Button
            {
                Text = "Cancelar",
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 10)
            };
            btnCancelar.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            pnlBotones.Controls.AddRange(new Control[] { btnAceptar, btnCancelar });
            pnlPrincipal.Controls.Add(pnlBotones, 0, 4);
        }
    }

    // ======================= FORM COMPARATIVO =======================
    public class FormComparativo : Form
    {
        public FormComparativo(List<ResultadoSimulacion> historial)
        {
            Text = "Reporte Comparativo de Algoritmos";
            Size = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;

            var pnlPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(15)
            };
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            Controls.Add(pnlPrincipal);

            // Título y botón exportar
            var pnlTitulo = new Panel { Dock = DockStyle.Fill };
            var lblTitulo = new Label
            {
                Text = "📊 Análisis Comparativo de Desempeño",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(0, 15),
                AutoSize = true,
                ForeColor = Color.FromArgb(156, 39, 176)
            };
            pnlTitulo.Controls.Add(lblTitulo);

            var btnExportar = new Button
            {
                Text = "Exportar Reporte",
                Size = new Size(150, 35),
                Location = new Point(920, 15),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnExportar.Click += (s, e) => ExportarReporte(historial);
            pnlTitulo.Controls.Add(btnExportar);

            pnlPrincipal.Controls.Add(pnlTitulo, 0, 0);

            // Tabla comparativa
            var dgvComparativo = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dgvComparativo.EnableHeadersVisualStyles = false;
            dgvComparativo.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(156, 39, 176);
            dgvComparativo.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvComparativo.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvComparativo.ColumnHeadersHeight = 35;

            dgvComparativo.Columns.Add("Ejecucion", "Ejecución");
            dgvComparativo.Columns.Add("Procesos", "Procesos");
            dgvComparativo.Columns.Add("Espera", "Espera Prom.");
            dgvComparativo.Columns.Add("Retorno", "Retorno Prom.");
            dgvComparativo.Columns.Add("CPU", "Tiempo CPU");
            dgvComparativo.Columns.Add("Simulacion", "T. Simulación");
            dgvComparativo.Columns.Add("Uso", "Uso CPU");

            for (int i = 0; i < historial.Count; i++)
            {
                var r = historial[i];
                dgvComparativo.Rows.Add(
                    $"Ejecución #{i + 1}",
                    r.ResultadosPorProceso.Count,
                    r.Estadisticas.PromedioEspera.ToString("0.##"),
                    r.Estadisticas.PromedioRetorno.ToString("0.##"),
                    r.Estadisticas.TiempoTotalCPU,
                    r.Estadisticas.TiempoTotalSimulacion,
                    r.Estadisticas.UtilizacionCPU.ToString("P1")
                );
            }

            pnlPrincipal.Controls.Add(dgvComparativo, 0, 1);

            // Análisis y conclusiones
            var pnlAnalisis = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(15),
                BorderStyle = BorderStyle.FixedSingle
            };

            var txtAnalisis = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.None,
                ScrollBars = ScrollBars.Vertical
            };

            // Generar análisis
            var mejorEspera = historial.OrderBy(h => h.Estadisticas.PromedioEspera).First();
            var mejorRetorno = historial.OrderBy(h => h.Estadisticas.PromedioRetorno).First();
            var mejorUso = historial.OrderBy(h => h.Estadisticas.UtilizacionCPU).Last();

            txtAnalisis.Text = $@"ANÁLISIS COMPARATIVO DE DESEMPEÑO
══════════════════════════════════════════════════════════════════════════════

Total de ejecuciones analizadas: {historial.Count}

🏆 MEJORES MÉTRICAS:

• Mejor Tiempo de Espera Promedio: {mejorEspera.Estadisticas.PromedioEspera:0.##} unidades
  (Ejecución con {mejorEspera.ResultadosPorProceso.Count} procesos)

• Mejor Tiempo de Retorno Promedio: {mejorRetorno.Estadisticas.PromedioRetorno:0.##} unidades
  (Ejecución con {mejorRetorno.ResultadosPorProceso.Count} procesos)

• Mejor Utilización de CPU: {mejorUso.Estadisticas.UtilizacionCPU:P2}
  (Ejecución con {mejorUso.ResultadosPorProceso.Count} procesos)

📈 ESTADÍSTICAS GENERALES:

• Espera promedio general: {historial.Average(h => h.Estadisticas.PromedioEspera):0.##} unidades
• Retorno promedio general: {historial.Average(h => h.Estadisticas.PromedioRetorno):0.##} unidades
• Uso de CPU promedio: {historial.Average(h => h.Estadisticas.UtilizacionCPU):P2}

💡 OBSERVACIONES:

El algoritmo MLQ con configuración multinivel permite gestionar eficientemente diferentes tipos 
de clientes bancarios, priorizando según su categoría (VIP, adultos mayores, embarazadas, 
regulares y foráneos).

La configuración de quantum en Round Robin afecta directamente los tiempos de espera y el 
cambio de contexto entre procesos. Valores más bajos favorecen la equidad pero aumentan el 
overhead del sistema.

RECOMENDACIONES:

• Para clientes prioritarios (VIP, adultos mayores, embarazadas): Usar FIFO o SJF en cola 1
• Para clientes regulares: Round Robin con quantum moderado (4-6) en cola 2
• Para clientes foráneos: SJF o FIFO en cola 3 para optimizar recursos";

            pnlAnalisis.Controls.Add(txtAnalisis);
            pnlPrincipal.Controls.Add(pnlAnalisis, 0, 2);
        }

        private void ExportarReporte(List<ResultadoSimulacion> historial)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Archivo de Texto|*.txt";
                sfd.FileName = $"reporte_comparativo_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (var writer = new System.IO.StreamWriter(sfd.FileName))
                        {
                            writer.WriteLine("REPORTE COMPARATIVO DE DESEMPEÑO");
                            writer.WriteLine("═══════════════════════════════════════════════════════════");
                            writer.WriteLine($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                            writer.WriteLine($"Total de ejecuciones: {historial.Count}\n");

                            var mejorEspera = historial.OrderBy(h => h.Estadisticas.PromedioEspera).First();
                            var mejorRetorno = historial.OrderBy(h => h.Estadisticas.PromedioRetorno).First();
                            var mejorUso = historial.OrderBy(h => h.Estadisticas.UtilizacionCPU).Last();

                            writer.WriteLine("MEJORES MÉTRICAS:");
                            writer.WriteLine($"• Mejor Espera: {mejorEspera.Estadisticas.PromedioEspera:0.##} unidades");
                            writer.WriteLine($"• Mejor Retorno: {mejorRetorno.Estadisticas.PromedioRetorno:0.##} unidades");
                            writer.WriteLine($"• Mejor Uso CPU: {mejorUso.Estadisticas.UtilizacionCPU:P2}\n");

                            writer.WriteLine("ESTADÍSTICAS GENERALES:");
                            writer.WriteLine($"• Espera promedio: {historial.Average(h => h.Estadisticas.PromedioEspera):0.##}");
                            writer.WriteLine($"• Retorno promedio: {historial.Average(h => h.Estadisticas.PromedioRetorno):0.##}");
                            writer.WriteLine($"• Uso CPU promedio: {historial.Average(h => h.Estadisticas.UtilizacionCPU):P2}\n");

                            writer.WriteLine("\nDETALLE DE EJECUCIONES:");
                            writer.WriteLine("═══════════════════════════════════════\n");

                            for (int i = 0; i < historial.Count; i++)
                            {
                                var h = historial[i];
                                writer.WriteLine($"Ejecución #{i + 1}:");
                                writer.WriteLine($"  Procesos: {h.ResultadosPorProceso.Count}");
                                writer.WriteLine($"  Espera promedio: {h.Estadisticas.PromedioEspera:0.##}");
                                writer.WriteLine($"  Retorno promedio: {h.Estadisticas.PromedioRetorno:0.##}");
                                writer.WriteLine($"  Uso CPU: {h.Estadisticas.UtilizacionCPU:P2}");
                                writer.WriteLine();
                            }
                        }
                        MessageBox.Show("Reporte exportado exitosamente.", "Éxito",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al exportar: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}