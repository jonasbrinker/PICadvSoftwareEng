using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.CodeDom;
using System.Timers;
using System.Windows.Threading;



namespace Pic_Simulator
{

 
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        public SimulationData data;
        //private ViewUpdater viewUpdater;
        //private SimulationController controller;

        double runTime = 0;
        private DispatcherTimer timer;
        bool run = false; 



        public MainWindow()
        {
            InitializeComponent();
            data = new SimulationData();
            Command.startUpRam();  
            PrintRam();
            PrintRaRb();
            PrintSTR();
            PrintOption();
            PrintINTCON();
            PrintStack();           
        }

        private void refreshUI()
        {
            PrintRam();
            refreshRAB();
            refreshSTR();
            refreshIntCon();
            refreshOption();
            refreshStack();
            lightLEDs();
        }
        
        private void LoadFile(object sender, RoutedEventArgs e)
        {
            LST_File.LoadFile(Stack, CodeScroller, data);
            Action jumpToStartCallback = () => LST_File.JumpToLine(Stack, 0);
            Command.ResetController(jumpToStartCallback);
            refreshUI();
            resetLEDs();


        }
        void selectedCellsChangedRA(object sender, RoutedEventArgs e)
        {
            ToggleBitInTable(RAGrid, data.tableRA,
                             value => Convert.ToInt32((string)value),
                             (newBit, colIndex) => {
                                 int ramBit = Command.SetSelectedBit(Command.ram[Command.bank, 5], Math.Abs(colIndex - 7), newBit);
                                 Command.ram[Command.bank, 5] = ramBit;
                             });
        }

        private void selectedCellsChangedSTR(object sender, RoutedEventArgs e)
        {
            ToggleBitInTable(STRGrid, data.tableSTR,
                             value => (int)value,
                             (newBit, colIndex) => {
                                 int ramBit = Command.SetSelectedBit(Command.ram[Command.bank, 3], Math.Abs(colIndex - 7), newBit);
                                 Command.ram[Command.bank, 3] = ramBit;
                             });
        }

        private void selectedCellsChangedINTCON(object sender, RoutedEventArgs e)
        {
            ToggleBitInTable(INTCONGrid, data.tableIntCon,
                             value => (int)value,
                             (newBit, colIndex) => {
                                 int ramBit = Command.SetSelectedBit(Command.ram[0, 11], Math.Abs(colIndex - 7), newBit);
                                 Command.ram[0, 11] = ramBit;
                             });
        }

        private void selectedCellsChangedOption(object sender, RoutedEventArgs e)
        {
            ToggleBitInTable(OptionGrid, data.tableOption,
                             value => (int)value,
                             (newBit, colIndex) => {
                                 int ramBit = Command.SetSelectedBit(Command.ram[1, 1], Math.Abs(colIndex - 7), newBit);
                                 Command.ram[1, 1] = ramBit;
                             });
        }

        void selectedCellsChangedRB(object sender, RoutedEventArgs e)
        {
            ToggleBitInTable(RBGrid, data.tableRB,
                             value => Convert.ToInt32((string)value),
                             (newBit, colIndex) => {
                                 int ramBit = Command.SetSelectedBit(Command.ram[Command.bank, 6], Math.Abs(colIndex - 7), newBit);
                                 Command.ram[Command.bank, 6] = ramBit;
                             });
        }

        private void ToggleBitInTable(DataGrid grid, DataTable table, Func<object, int> valueConverter, Action<int, int> updateRam)
        {
            int rowIndex = grid.Items.IndexOf(grid.CurrentItem);
            int colIndex = grid.CurrentCell.Column.DisplayIndex;
            object cellValue = table.Rows[rowIndex][colIndex];
            int intValue = valueConverter(cellValue);

            table.Rows[rowIndex][colIndex] = (intValue == 0) ? 1 : 0;
            int newBit = (intValue == 0) ? 1 : 0;

            updateRam(newBit, colIndex);
            refreshUI();
        }

        private void refreshRAB()
        {
            for (int i = 7; i >= 0; i--)
            {
                data.tableRA.Rows[0][i] = Command.GetSelectedBit(Command.ram[0, 5], Math.Abs(i-7)).ToString() ;
                data.tableRB.Rows[0][i] = Command.GetSelectedBit(Command.ram[0, 6], Math.Abs(i - 7)).ToString();
                int trisA =  Command.GetSelectedBit(Command.ram[1, 5], Math.Abs(i - 7));
                if(trisA == 0)
                {
                    data.tableRA.Rows[1][i] = "o"; 
                }else
                {
                    data.tableRA.Rows[1][i] = "i";
                }
                int trisB = Command.GetSelectedBit(Command.ram[1, 6], Math.Abs(i - 7));
                if (trisB == 0)
                {
                    data.tableRB.Rows[1][i] = "o";
                }
                else
                {
                    data.tableRB.Rows[1][i] = "i";
                }
            }
        }

        private void refreshSTR()
        {
            for (int i = 7; i >= 0; i--)
            {
                data.tableSTR.Rows[0][i] = Command.GetSelectedBit(Command.ram[Command.bank, 3], Math.Abs(i - 7));
                
            }
        }

        private void refreshStack()
        {
            for (int i = 0; i < 8; i++)
            {
                data.tableStack.Rows[i][0] = Command.callStack[i];

            }
            CallPos.Text = Command.callPosition.ToString();
        }

        private void refreshIntCon()
        {
            for (int i = 7; i >= 0; i--)
            {
                data.tableIntCon.Rows[0][i] = Command.GetSelectedBit(Command.ram[0, 11], Math.Abs(i - 7));

            }
        }

        private void refreshOption()
        {
            for (int i = 7; i >= 0; i--)
            {
                data.tableOption.Rows[0][i] = Command.GetSelectedBit(Command.ram[1, 1], Math.Abs(i - 7));

            }
        }

        private void RunButton(object sender, RoutedEventArgs e)
        {
            
            if (!run)
            {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(0.3);

                // Füge den Event-Handler für das Tick-Ereignis hinzu
                timer.Tick += Run;

                // Starte den Timer
                timer.Start();
                run = true;
                runButton.Background = Brushes.LightGreen; 
            }
            else
            {
                timer.Stop();
                run = false;
                runButton.Background = Brushes.LightGray;
            }
        }

        private void Run(object sender, EventArgs e)
        {
            bool breakpointactive = false; 
            foreach (var breakpoint in LST_File.breakpoints)
            {
                
                int lineIndex = breakpoint.Key;
                if (LST_File.pos == lineIndex)
                {
                    breakpointactive = true;                 
                }
            }
            if (!breakpointactive)
            {               
                OneStep(null, null);
            }
              
        }


        private void OneStep(object sender, RoutedEventArgs e)
        {
            if (!LST_File.loadedFile) return;
            if (LST_File.pos >= LST_File.fileSize) return;
            if (LST_File.CheckCommand(Stack) == false)
            {
                LST_File.MarkLine(Stack, CodeScroller);
                return;
            };
            if(!Command.sleepModus)
            {
                int command = Fetch();
                if (!Decode(command)) return;
                if(!Command.sleepModus)LST_File.MarkLine(Stack, CodeScroller);
                Command.EEPROM();
            } 
            Result.Text = "";
            Command.CheckWriteEEPROM();
            Command.Mirroring();
            Action<int> jumpCallback = (lineNumber) => LST_File.JumpToLine(Stack, lineNumber);
            Command.Interrupts(jumpCallback);
            if (Command.sleepModus)
            {
                Action resetCallback = () => Command.ResetController(() => LST_File.JumpToLine(Stack, 0));
                Action<string, string> messageCallback = (text, title) => MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
                Command.Watchdog(resetCallback, messageCallback, 1);
                displayrunTime(1);
            }
            Result.Text = Result.Text + "\n" + "W-Register: " + Command.wReg + "\n" + "Watchdog: " + Command.watchdog + "\n" + "PCL: " + (Command.PCLATH & 0xFF) + "\n" + "PCLATH: " + (Command.PCLATH & 0x1F00) + "\n" + "SFR: " + (Command.ram[0,4]);
            refreshUI();
        }

        private void resetLEDs()
        {
            LEDOne.Fill = new SolidColorBrush(Colors.LightGray);
            LEDOTwo.Fill = new SolidColorBrush(Colors.LightGray);
            LEDThree.Fill = new SolidColorBrush(Colors.LightGray);
            LEDFour.Fill = new SolidColorBrush(Colors.LightGray);
            LEDFive.Fill = new SolidColorBrush(Colors.LightGray);
            LEDSix.Fill = new SolidColorBrush(Colors.LightGray);
            LEDSeven.Fill = new SolidColorBrush(Colors.LightGray);
            LEDEight.Fill = new SolidColorBrush(Colors.LightGray);
        }
        
        private void lightLEDs()
        {
            int port = 6; // this can be changed weather its PortA or PortB, needs to implemented later

            
            int intValue= Command.ram[Command.bank, port]; 

            for(int i = 0; i < 8; i++)
            {
                int LED = Command.GetSelectedBit(intValue, i); 
                int isOutputValue = Command.ram[1, port];
                int LEDisOutput = Command.GetSelectedBit(isOutputValue, i);
                if(LEDisOutput == 0)
                {
                    switch (i)
                    {
                        case 0:
                            if (LED == 0)
                            {
                                LEDOne.Fill = new SolidColorBrush(Colors.LightGray);
                            }
                            else
                            {
                                LEDOne.Fill = new SolidColorBrush(Colors.Red);
                            }
                            break;
                        case 1:
                            if (LED == 0)
                            {
                                LEDOTwo.Fill = new SolidColorBrush(Colors.LightGray);
                            }
                            else
                            {
                                LEDOTwo.Fill = new SolidColorBrush(Colors.Red);
                            }
                            break;
                        case 2:
                            if (LED == 0)
                            {
                                LEDThree.Fill = new SolidColorBrush(Colors.LightGray);
                            }
                            else
                            {
                                LEDThree.Fill = new SolidColorBrush(Colors.Red);
                            }
                            break;
                        case 3:
                            if (LED == 0)
                            {
                                LEDFour.Fill = new SolidColorBrush(Colors.LightGray);
                            }
                            else
                            {
                                LEDFour.Fill = new SolidColorBrush(Colors.Red);
                            }
                            break;
                        case 4:
                            if (LED == 0)
                            {
                                LEDFive.Fill = new SolidColorBrush(Colors.LightGray);
                            }
                            else
                            {
                                LEDFive.Fill = new SolidColorBrush(Colors.Red);
                            }
                            break;
                        case 5:
                            if (LED == 0)
                            {
                                LEDSix.Fill = new SolidColorBrush(Colors.LightGray);
                            }
                            else
                            {
                                LEDSix.Fill = new SolidColorBrush(Colors.Red);
                            }
                            break;
                        case 6:
                            if (LED == 0)
                            {
                                LEDSeven.Fill = new SolidColorBrush(Colors.LightGray);
                            }
                            else
                            {
                                LEDSeven.Fill = new SolidColorBrush(Colors.Red);
                            }
                            break;
                        case 7:
                            if (LED == 0)
                            {
                                LEDEight.Fill = new SolidColorBrush(Colors.LightGray);
                            }
                            else
                            {
                                LEDEight.Fill = new SolidColorBrush(Colors.Red);
                            }
                            break;


                    }
                }
                
            }

        }

        


        private void PrintRaRb()
        {

            for (int i = 7; i >= 0; i--)
            {
                data.tableRA.Columns.Add("RA" + i.ToString(), typeof(string));
            }
            int storageRA = Command.ram[Command.bank, 5];
            DataRow rowRA = data.tableRA.NewRow();
            int j = 0;
            for (int i = 7; i >= 0; i--)
            {
                rowRA[j] = Command.GetSelectedBit(Command.ram[Command.bank, 5], i).ToString();
                j++;
            }
            data.tableRA.Rows.Add(rowRA);

            DataRow rowTrisRA = data.tableRA.NewRow();
            j = 0;
            for (int i = 7; i >= 0; i--)
            {
                int value = Command.GetSelectedBit(Command.ram[1, 5], i);
                if (value == 0)
                {
                    rowTrisRA[j] = "o";
                }
                else
                {
                    rowTrisRA[j] = "i";
                }
                j++;
            }
            data.tableRA.Rows.Add(rowTrisRA);

            RAGrid.ItemsSource = data.tableRA.DefaultView;


            // Füge Spalten für RB0 bis RB7 hinzu
            for (int i = 7; i >= 0; i--)
            {
                data.tableRB.Columns.Add("RB" + i.ToString(), typeof(string));
            }

            DataRow rowRB = data.tableRB.NewRow();
            int k = 0; 
            for (int i = 7; i >= 0; i--)
            {
                rowRB[k] = Command.GetSelectedBit(Command.ram[Command.bank, 6], i).ToString();
                k++; 
            }
            data.tableRB.Rows.Add(rowRB);

            DataRow rowTrisRB = data.tableRB.NewRow();
            k = 0;
            for (int i = 7; i >= 0; i--)
            {
                int value = Command.GetSelectedBit(Command.ram[1, 6], i);
                if(value == 0)
                {
                    rowTrisRB[k] = "o";
                }
                else
                {
                    rowTrisRB[k] = "i";
                }                
                k++;
            }
            data.tableRB.Rows.Add(rowTrisRB);
            RBGrid.ItemsSource = data.tableRB.DefaultView;

            

            

        }

        private void PrintStack()
        {
            data.tableStack.Columns.Add("Stack", typeof(int)); 
            
             
            for (int i= 0; i < 8; i++)
            {
                DataRow row = data.tableStack.NewRow();
                row[0] = Command.callStack[i];
                data.tableStack.Rows.Add(row);
            }
            
            StackGrid.ItemsSource = data.tableStack.DefaultView;
            CallPos.Text = Command.callPosition.ToString();
        }

        private void PrintSTR()
        {

            data.tableSTR.Columns.Add("IRP", typeof(int));
            data.tableSTR.Columns.Add("RP1", typeof(int));
            data.tableSTR.Columns.Add("RP0" , typeof(int));
            data.tableSTR.Columns.Add("TO", typeof(int));
            data.tableSTR.Columns.Add("PD", typeof(int));
            data.tableSTR.Columns.Add("Z", typeof(int));
            data.tableSTR.Columns.Add("D", typeof(int));
            data.tableSTR.Columns.Add("C", typeof(int));


            DataRow row = data.tableSTR.NewRow();
            int k = 0;
            for (int i = 7; i >= 0; i--)
            {
                row[k] = Command.GetSelectedBit(Command.ram[Command.bank, 3], i);
                k++;
            }
            data.tableSTR.Rows.Add(row);
            STRGrid.ItemsSource = data.tableSTR.DefaultView;
        }

        private void PrintINTCON()
        {

            data.tableIntCon.Columns.Add("GIE", typeof(int));
            data.tableIntCon.Columns.Add("EEIE", typeof(int));
            data.tableIntCon.Columns.Add("T0IE", typeof(int));
            data.tableIntCon.Columns.Add("INTE", typeof(int));
            data.tableIntCon.Columns.Add("RBIE", typeof(int));
            data.tableIntCon.Columns.Add("T0IF", typeof(int));
            data.tableIntCon.Columns.Add("INTF", typeof(int));
            data.tableIntCon.Columns.Add("RBIF", typeof(int));


            DataRow row = data.tableIntCon.NewRow();
            int k = 0;
            for (int i = 7; i >= 0; i--)
            {
                row[k] = Command.GetSelectedBit(Command.ram[Command.bank, 11], i);
                k++;
            }
            data.tableIntCon.Rows.Add(row);
            INTCONGrid.ItemsSource = data.tableIntCon.DefaultView;
        }

        private void PrintOption()
        {

            data.tableOption.Columns.Add("RBPU", typeof(int));
            data.tableOption.Columns.Add("INTEDG", typeof(int));
            data.tableOption.Columns.Add("T0CS", typeof(int));
            data.tableOption.Columns.Add("T0SE", typeof(int));
            data.tableOption.Columns.Add("PSA", typeof(int));
            data.tableOption.Columns.Add("PS2", typeof(int));
            data.tableOption.Columns.Add("PS1", typeof(int));
            data.tableOption.Columns.Add("PS0", typeof(int));


            DataRow row = data.tableOption.NewRow();
            int k = 0;
            for (int i = 7; i >= 0; i--)
            {
                row[k] = Command.GetSelectedBit(Command.ram[1, 1], i);
                k++;
            }
            data.tableOption.Rows.Add(row);
            OptionGrid.ItemsSource = data.tableOption.DefaultView;
        }

        private void PrintRam()
        {
            DataTable dt = new DataTable();
            int nbColumns = 8;
            int nbRows = 32;

            for (int i = 0; i < nbColumns; i++)
            {
                dt.Columns.Add(i.ToString(), typeof(string));
            }
            int zaehler = 0;
            int tmpBank = 0;
            for (int row = 0; row < nbRows; row++)
            {
                DataRow dr = dt.NewRow();            
                for (int i = 0; i < nbColumns; i++)
                {
                    dr[i] = Command.ram[tmpBank, zaehler].ToString("X");
                    zaehler++;

                }
                if (zaehler == 128)
                {
                    zaehler = 0;
                    tmpBank = 1;
                }
                dt.Rows.Add(dr);
                
            }
            MyDataGrid.ItemsSource = dt.DefaultView;
            dt.RowChanged += dtRowChanged;
            

        }

        private void dtRowChanged(object sender, DataRowChangeEventArgs e)
        {
            data.UpdateRamFromRow(e.Row);
        }


        private int Fetch()
        {
            int programCounter = Command.ram[Command.bank, 2];
            int command = data.commands[programCounter];
            programCounter++;
            Command.ChangePCLATH(Command.ram[Command.bank, 2] + 1);
            return command;
        }
        private void displayrunTime(int deltaT)
        {

            runTime += ((deltaT * 4000000.00) / Command.quarzfrequenz);
            Laufzeitzaehler.Text = runTime.ToString();
        }


        private bool Decode(int command)
        {
            InstructionProcessor processor = Command.GetInstructionProcessor();
            int deltaT = 0;
            if ((command & 0x3F00) == 0x3000)
            {
                deltaT = processor.MOVLW(command & 0xFF);
            }
            if ((command & 0x3F80) == 0x0080)
            {
                deltaT = processor.MOVWF(command & 0x7F);
            }
            if ((command & 0x3F80) == 0x0780 || (command & 0x3F80) == 0x0700)
            {
                deltaT = processor.ADDWF(command & 0xFF);
            }
            if ((command & 0x3F80) == 0x0500 || (command & 0x3F80) == 0x0580)
            {
                deltaT = processor.ANDWF(command & 0xFF);
            }
            if ((command & 0x3F00) == 0x3E00)
            {
                deltaT = processor.ADDLW(command & 0xFF);
            }
            if ((command & 0x3F00) == 0x3900)
            {
                deltaT = processor.ANDLW(command & 0xFF);
            }
            if ((command & 0x3F80) == 0x0180)
            {
                deltaT = processor.CLRF(command & 0x7F);
            }
            if ((command & 0x3F80) == 0x0100)
            {
                deltaT = processor.CLRW();
            }
            if ((command & 0x3F80) == 0x0980 || (command & 0x3F80) == 0x0900)
            {
                deltaT = processor.COMF(command & 0xFF);
            }
            if ((command & 0x3F80) == 0x0380 || (command & 0x3F80) == 0x0300)
            {
                deltaT = processor.DECF(command & 0xFF);
            }
            if ((command & 0x3800) == 0x2000)
            {
                deltaT = processor.CALL(command & 0xFF, Stack);
            }
            if ((command & 0xFFFF) == 0x0008)
            {
                deltaT = processor.RETURN(Stack);
            }
            if ((command & 0x3800) == 0x2800)
            {
                deltaT = processor.GOTO(command & 0x7FF, Stack);
            }
            if ((command & 0xFC00) == 0x3400)
            {
                deltaT = processor.RETLW(command & 0xFF, Stack);
            }
            if ((command & 0x3F80) == 0x0B80 || (command & 0x3F80) == 0x0B00)
            {
                deltaT = processor.DECFSZ(command & 0xFF, Stack);
            }
            if ((command & 0x3F80) == 0x0A80 || (command & 0x3F80) == 0x0A00)
            {
                deltaT = processor.INCF(command & 0xFF);
            }
            if ((command & 0x3F80) == 0x0F80 || (command & 0x3F80) == 0xF00)
            {
                deltaT = processor.INCFSZ(command & 0xFF, Stack);
            }
            if ((command & 0x3F80) == 0x0480 || (command & 0x3F80) == 0x0400)
            {
                deltaT = processor.IORWF(command & 0xFF);
            }
            if ((command & 0x3F80) == 0x0880 || (command & 0x3F80) == 0x0800)
            {
                deltaT = processor.MOVF(command & 0xFF);
            }
            if ((command & 0xFFFF) == 0x0000)
            {
                deltaT = processor.NOP();
            }
            if ((command & 0x3F80) == 0x0D80 || (command & 0x3F80) == 0x0D00)
            {
                deltaT = processor.RLF(command & 0xFF);
            }
            if ((command & 0x3F80) == 0x0C80 || (command & 0x3F80) == 0x0C00)
            {
                deltaT = processor.RRF(command & 0xFF);
            }
            if ((command & 0x3F80) == 0x0680 || (command & 0x3F80) == 0x0600)
            {
                deltaT = processor.XORWF(command & 0xFF);
            }
            if ((command & 0x3F00) == 0x3A00)
            {
                deltaT = processor.XORLW(command & 0xFF);
            }
            if ((command & 0x3C00) == 0x1000)
            {
                deltaT = processor.BCF(command & 0x03FF);
            }
            if ((command & 0x3C00) == 0x1400)
            {
                deltaT = processor.BSF(command & 0x03FF);
            }
            if ((command & 0x3C00) == 0x1800)
            {
                deltaT = processor.BTFSC(command & 0x03FF, Stack);
            }
            if ((command & 0x3C00) == 0x1C00)
            {
                deltaT = processor.BTFSS(command & 0x03FF, Stack);
            }
            if ((command & 0x3F00) == 0x0E00)
            {
                deltaT = processor.SWAPF(command & 0xFF);
            }
            if ((command & 0x3F80) == 0x0280 || (command & 0x3F80) == 0x0200)
            {
                deltaT = processor.SUBWF(command & 0xFF);
            }
            if ((command & 0x3F00) == 0x3800)
            {
                deltaT = processor.IORLW(command & 0xFF);
            }
            if ((command & 0x3F00) == 0x3C00)
            {
                deltaT = processor.SUBLW(command & 0xFF);
            }
            if((command & 0xFFFF) == 0x0060)
            {
                deltaT = processor.CLRWDT();
            }
            if((command & 0xFFFF) == 0x0009)
            {
                deltaT = processor.RETFIE(Stack);
            }
            if((command & 0xFFFF) == 0x0063)
            {
                Command.SLEEP();
            }

            Action<int> jumpCallback = (lineNumber) => LST_File.JumpToLine(Stack, lineNumber);
            if (!((command & 0x3F80) == 0x0080 && (command & 0x7F) == 1)) Command.Timer0(jumpCallback, deltaT);
            Action resetCallback = () => Command.ResetController(() => LST_File.JumpToLine(Stack, 0));
            Action<string, string> messageCallback = (text, title) => MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
            Command.Watchdog(resetCallback, messageCallback, deltaT);
            displayrunTime(deltaT);
            return true;
        }

        private void quarzfrequenz_Four(object sender, RoutedEventArgs e)
        {
            Command.setQuarzfrequenz(4000000);
        }

        private void quarzfrequenz_Eight(object sender, RoutedEventArgs e)
        {
            Command.setQuarzfrequenz(8000000);
        }

        private void quarzfrequenz_Sixteen(object sender, RoutedEventArgs e)
        {
            Command.setQuarzfrequenz(16000000);
        }

        private void quarzfrequenz_Thrittwo(object sender, RoutedEventArgs e)
        {
            Command.setQuarzfrequenz(32000);
        }


        
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            Action jumpToStartCallback = () => LST_File.JumpToLine(Stack, 0);
            Command.ResetController(jumpToStartCallback);
            PrintRam();
            refreshRAB();
            refreshSTR();
            refreshIntCon();
            refreshOption();
            refreshStack();
            lightLEDs();
        }
    }
}
