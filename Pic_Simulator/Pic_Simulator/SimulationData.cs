using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pic_Simulator
{
    public class SimulationData
    {
        public List<int> commands = new List<int>();
        public DataTable tableRB = new DataTable();
        public DataTable tableRA = new DataTable();
        public DataTable tableSTR = new DataTable();
        public DataTable tableIntCon = new DataTable();
        public DataTable tableOption = new DataTable();
        public DataTable tableStack = new DataTable();


        public String[] ConvertRowToIntArray(DataRow row)
        {
            // Neues int-Array erstellen
            String[] intArray = new String[row.ItemArray.Length];

            // Daten aus der DataRow in das int-Array kopieren
            for (int i = 0; i < row.ItemArray.Length; i++)
            {
                intArray[i] = Convert.ToString(row[i]);
            }

            return intArray;
        }

        public void UpdateRamFromRow(DataRow row)
        {
            DataTable table = row.Table;
            int rowIndex = table.Rows.IndexOf(row);
            string[] intArray = ConvertRowToIntArray(row);

            int i = 0;
            if (rowIndex > 15)
            {
                i = 1;
                rowIndex = rowIndex - 16; // Das muss gemacht werden da es im dargestellten ram alles in einer Tabelle hängt aber im speicher aufgeteilt wird auf Command.bank 1 und 0
            }
            int rowstart = rowIndex * 8;

            for (int j = 0; j < 8; j++)
            {
                if (Convert.ToInt32(intArray[j], 16) > 255)
                {
                    Command.ram[i, (rowstart + j)] = 0;
                }
                else
                {
                    Command.ram[i, (rowstart + j)] = Convert.ToInt32(intArray[j], 16);
                }

                Trace.WriteLine(Command.ram[i, (rowstart + j)]);
            }
        }
    }
}
