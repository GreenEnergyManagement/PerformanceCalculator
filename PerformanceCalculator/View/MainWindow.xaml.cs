using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace PerformanceCalculator.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SortedDictionary<int, SortedDictionary<UtcDateTime, BinSkillScore>> table = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Calculate(object sender, RoutedEventArgs e)
        {
            int forecastTimeIndex = int.Parse(txtForecastColTimeIndex.Text);
            int forecastValueIndex = int.Parse(txtForecastColValueIndex.Text);
            int offsetHoursAhead = int.Parse(txtHoursAheadOffset.Text);
            char forecastSep = txtForecastColSeparator.Text[0];

            int observationTimeIndex = int.Parse(txtObservationColTimeIndex.Text);
            int observationValueIndex = int.Parse(txtObservationColValueIndex.Text);
            double normValue = double.Parse(txtNormalizationValue.Text, NumberStyles.Float, CultureInfo.InvariantCulture);
            char observationSep = txtObservationColSeparator.Text[0];

            string fcastUnitType = ((ComboBoxItem) forecastUnitType.SelectedItem).Content.ToString();
            string obsUnitType = ((ComboBoxItem) obsevationsUnitType.SelectedItem).Content.ToString();
            string statMode = ((ComboBoxItem) cmbBoxStatMethod.SelectedItem).Content.ToString();

            bool hasData = Directory.Exists(txtForecastPath.Text) && File.Exists(txtObsevationsPath.Text);
            if (hasData)
            {
                try
                {
                    var fmd = new ForecastMetaData(forecastTimeIndex, forecastValueIndex, offsetHoursAhead, forecastSep, fcastUnitType);
                    var omd = new ObservationMetaData(observationTimeIndex, observationValueIndex, observationSep, obsUnitType, normValue);

                    var result = PerformanceCalculatorEngine.Calculate(fmd, new DirectoryInfo(txtForecastPath.Text), omd, txtObsevationsPath.Text);

                    table = new SortedDictionary<int, SortedDictionary<UtcDateTime, BinSkillScore>>();
                    foreach (var t in result.Keys)
                    {
                        var isoT = t;
                        HourlySkillScoreCalculator calculator;
                        result.TryGetValue(t, out calculator);
                        if (calculator != null)
                        {
                            foreach (BinSkillScore skill in calculator.GetSkillScoreBins())
                            {
                                if (!table.ContainsKey(skill.SkillId)) table.Add(skill.SkillId, new SortedDictionary<UtcDateTime, BinSkillScore>());
                                if (!table[skill.SkillId].ContainsKey(isoT)) table[skill.SkillId].Add(isoT, skill);
                            }
                        }
                    }

                    DisplayData(statMode);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                    Console.WriteLine(ex.StackTrace);
                }
            }
            else MessageBox.Show("The forecasts path must point to a parent folder \rcontaining forecasts, while the observations file must \rpoint to an exact file containing a time series \rof observations.", "Error");
        }

        public void DisplayData(string statMode) 
        {
            var dt = new DataTable();
            string heading = string.Empty;
            foreach (var hour in table.Keys)
            {
                string line = string.Empty;
                bool buildHeading = string.IsNullOrEmpty(heading);
                int index = 0;
                foreach (var skill in table[hour])
                {
                    if (index == 0) line += skill.Value.Print(statMode);
                    else line += "\t " + BinSkillScore.FormatDouble(skill.Value.GetSkill().Get(statMode));

                    var time = skill.Key;
                    if (buildHeading && string.IsNullOrEmpty(heading)) heading += time.UtcTime.ToString("\t \t dd/MM/yyyy", CultureInfo.InvariantCulture);
                    else if (buildHeading && !string.IsNullOrEmpty(heading)) heading += time.UtcTime.ToString("\t dd/MM/yyyy", CultureInfo.InvariantCulture);
                    index++;
                }

                if (buildHeading)
                {   // create column header
                    int col = 0;
                    foreach (string s in heading.Split('\t'))
                    {
                        string colName = s;
                        if (col == 0) colName = "Hour Ahead";
                        else if (col == 1) colName = "Method";
                        else
                        {
                            var dateParts = colName.Split('/');
                            int month = int.Parse(dateParts[1]);
                            string monthAbb = GetMonthAbbrevation(month);
                            colName = monthAbb + dateParts[2];
                        }

                        dt.Columns.Add(new DataColumn(colName, typeof(string)));
                        col++;
                    }
                }

                // Add data to DataTable
                DataRow newRow = dt.NewRow();
                int column = 0;
                foreach (var data in line.Split('\t'))
                {
                    string val = data;
                    if (column > 1) val = val.Replace(',', '.');
                    newRow[column] = val;
                    column++;
                }
                dt.Rows.Add(newRow);
            }

            grid.ItemsSource = dt.DefaultView;
        }

        private string GetMonthAbbrevation(int month)
        {
            if (month == 1) return "Jan";
            if (month == 2) return "Feb";
            if (month == 3) return "Mar";
            if (month == 4) return "Apr";
            if (month == 5) return "May";
            if (month == 6) return "Jun";
            if (month == 7) return "Jul";
            if (month == 8) return "Aug";
            if (month == 9) return "Sep";
            if (month == 10) return "Oct";
            if (month == 11) return "Nov";
            if (month == 12) return "Dec";
            throw new Exception("Unkown month number: "+month);
        }

        private void DisplayDataForStatMode(object sender, SelectionChangedEventArgs e)
        {
            string statMode = ((ComboBoxItem)cmbBoxStatMethod.SelectedItem).Content.ToString();
            if (table != null) DisplayData(statMode);
        }
        //C:\aquiloz\awpp\awpp\Aquiloz.Service.PowerPrediction.Test\bin\Debug\Roedsand_2\2014
        //C:\Dump\Rodsand\MeasuredMay-June.csv
    }
}
