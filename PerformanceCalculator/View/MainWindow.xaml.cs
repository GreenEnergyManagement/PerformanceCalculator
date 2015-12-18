using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PerformanceCalculator.SkillScore;

namespace PerformanceCalculator.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SortedDictionary<int, SortedDictionary<UtcDateTime, BinSkillScore>> table = null;
        private bool includeNegObs = false;
        private bool useFixedHours = false;
        private BackgroundWorker bw;

        public MainWindow()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            InitializeComponent();
        }

        public bool IsAvailable()
        {
            return !UiServices.IsBusy;
        }

        private void Calculate(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
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

                string filter = "*.csv";
                string forecastPath = txtForecastPath.Text;
                int index = forecastPath.IndexOf('*');
                if (index != -1)
                {
                    filter = forecastPath.Substring(index);
                    forecastPath = forecastPath.Substring(0, index);
                }
                string observationFilePath = txtObsevationsPath.Text;

                var scope = GetScope();
                bool hasData = Directory.Exists(forecastPath) && File.Exists(observationFilePath);
                if (hasData)
                {
                    try
                    {
                        var fmd = new ForecastMetaData(forecastTimeIndex, forecastValueIndex, offsetHoursAhead, forecastSep, fcastUnitType, filter);
                        var omd = new ObservationMetaData(observationTimeIndex, observationValueIndex, observationSep, obsUnitType, normValue);

                        SetBusy(true);

                        bw = new BackgroundWorker();
                        bw.DoWork += BwCalculatePerformance;
                        bw.RunWorkerCompleted += BwCalculatePerformanceCompleted;

                        dynamic args = new
                        {
                            Fmd = fmd,
                            ForecastPath = forecastPath,
                            Omd = omd,
                            ObservationFilePath = observationFilePath,
                            Scope = scope.ToArray(),
                            IncludeNegObs = includeNegObs,
                            UseFixedHours = useFixedHours,
                            SiteId = siteId
                        };
                        bw.RunWorkerAsync(args);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error");
                        Console.WriteLine(ex.StackTrace);
                    }
                }
                else if (File.Exists(forecastPath))
                {
                    try
                    {
                        var fmd = new ForecastMetaData(forecastTimeIndex, forecastValueIndex, offsetHoursAhead, forecastSep, fcastUnitType, filter);
                        var omd = new ObservationMetaData(observationTimeIndex, observationValueIndex, observationSep, obsUnitType, normValue);

                        bw = new BackgroundWorker();
                        bw.DoWork += BwCalculatePerformance;
                        bw.RunWorkerCompleted += BwCalculatePerformanceCompleted;

                        dynamic args = new
                        {
                            Fmd = fmd,
                            ForecastPath = forecastPath,
                            Omd = omd,
                            ObservationFilePath = observationFilePath,
                            Scope = scope.ToArray(),
                            IncludeNegObs = includeNegObs,
                            UseFixedHours = useFixedHours,
                            SiteId = siteId
                        };
                        bw.RunWorkerAsync(args);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error");
                        Console.WriteLine(ex.StackTrace);
                    }
                }
                else
                    MessageBox.Show(
                        "The forecasts path must point to a parent folder \rcontaining forecasts, while the observations file must \rpoint to an exact file containing a time series \rof observations.",
                        "Error");
            }
        }

        private void SetBusy(bool busy)
        {
            UiServices.SetBusyState(busy);
            foreach (var ctrl in GetChildren((Visual)VisualTreeHelper.GetChild(this, 0)))
            {
                string s = ctrl.GetType().Name;
                if (s.Equals("TextBox")) ((TextBox)ctrl).IsEnabled = !busy;
                if (s.Equals("CheckBox")) ((CheckBox)ctrl).IsEnabled = !busy;
                if (s.Equals("ComboBox")) ((ComboBox)ctrl).IsEnabled = !busy;
                if (s.Equals("Button")) ((Button)ctrl).IsEnabled = !busy;
            }
        }

        public static IEnumerable<Visual> GetChildren(Visual parent, bool recurse = true)
        {
            if (parent != null)
            {
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    // Retrieve child visual at specified index value.
                    var child = VisualTreeHelper.GetChild(parent, i) as Visual;

                    if (child != null)
                    {
                        yield return child;

                        if (recurse)
                        {
                            foreach (var grandChild in GetChildren(child, true))
                            {
                                yield return grandChild;
                            }
                        }
                    }
                }
            }
        }

        private void BwCalculatePerformance(object sender, DoWorkEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
             
            dynamic args = e.Argument;
            var fmd = args.Fmd;
            var forecastPath = args.ForecastPath;
            var omd = args.Omd;
            var observationFilePath = args.ObservationFilePath;
            var scope = args.Scope;
            var includeNegObservations = args.IncludeNegObs;
            var useFixedHourz = args.UseFixedHours;
            var site = args.SiteId;
            var result = PerformanceCalculatorEngine.Calculate(fmd, forecastPath, omd, observationFilePath, scope, includeNegObservations, useFixedHourz, site);

            e.Result = result;
        }

        private void BwCalculatePerformanceCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var result = new ConcurrentDictionary<UtcDateTime, HourlySkillScoreCalculator>();
            try
            {
                result = (ConcurrentDictionary<UtcDateTime, HourlySkillScoreCalculator>) e.Result;
            }
            catch (Exception) { }

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

            string statMode = ((ComboBoxItem)cmbBoxStatMethod.SelectedItem).Content.ToString();
            DisplayData(statMode);
            SetBusy(false);
        }

        private List<int> GetScope()
        {
            string scopeStr = txtScope.Text;
            string[] scopeSteps = scopeStr.Split(',');
            var scope = new List<int>();
            scope.Add(-1);
            foreach (var scopeStep in scopeSteps)
            {
                var step = scopeStep.Trim();
                if (step.Contains("-"))
                {
                    var firstAndLast = step.Split('-');
                    int first = int.Parse(firstAndLast[0]);
                    int last = int.Parse(firstAndLast[1]);
                    int count = last - first;
                    scope.AddRange(Enumerable.Range(first, count + 1));
                }
                else
                {
                    int nr = int.Parse(step);
                    scope.Add(nr);
                }
            }
            return scope;
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
                        else if (col == 1) colName = "Metric";
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
                    if (column > 1)
                    {
                        val = val.Replace(',', '.');
                        double value;
                        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        {
                            newRow[column] = value;
                        }
                        else throw new Exception("Unable to convert text: '"+val+"' to a double value.");
                    }
                    else newRow[column] = val;
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

        private void FillGuiWithExampleData(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                txtForecastPath.Text = @".\Data\SampleForecasts";
                txtObsevationsPath.Text = @".\Data\SampleObservations.csv";
                txtForecastColTimeIndex.Text = "1";
                txtForecastColValueIndex.Text = "5";
                txtNormalizationValue.Text = "207000";
            }
            /*txtForecastPath.Text = @".\Data\Validation\DA_Forecasts_Reinsbuettel";
            txtObsevationsPath.Text = @".\Data\Validation\HistDaten_Reinsbuettel_2014_utc.csv";
            txtForecastColTimeIndex.Text = "1";
            txtForecastColValueIndex.Text = "5";
            forecastUnitType.SelectedIndex = 1;
            txtForecastColSeparator.Text = ";";
            txtObservationColSeparator.Text = ";";
            txtScope.Text = "0-23";
            obsevationsUnitType.SelectedIndex = 0;
            txtNormalizationValue.Text = "13500";*/
        }

        private void FillGuiWithH1(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                txtForecastPath.Text = @".\Data\Holste-Lohe1";
                txtObsevationsPath.Text = @".\Data\Holste1FarmProduction.csv";
                txtForecastColTimeIndex.Text = "0";
                txtForecastColValueIndex.Text = "6";
                txtNormalizationValue.Text = "18000";

                forecastUnitType.SelectedIndex = 1;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "13-40";
            }
        }

        private void FillGuiWithH2(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                txtForecastPath.Text = @".\Data\Holste-Lohe2";
                txtObsevationsPath.Text = @".\Data\Holste2FarmProduction.csv";
                txtForecastColTimeIndex.Text = "0";
                txtForecastColValueIndex.Text = "6";
                txtNormalizationValue.Text = "7200";

                forecastUnitType.SelectedIndex = 1;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "13-40";
            }
        }

        private void FillGuiWithSchinne(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                txtForecastPath.Text = @".\Data\Schinne";
                txtObsevationsPath.Text = @".\Data\SchinneFarmProduction.csv";
                txtForecastColTimeIndex.Text = "0";
                txtForecastColValueIndex.Text = "6";
                txtNormalizationValue.Text = "58600";

                forecastUnitType.SelectedIndex = 1;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "13-40";
            }
        }

        private void FillGuiWithStoessen(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                txtForecastPath.Text = @".\Data\Stoessen";
                txtObsevationsPath.Text = @".\Data\StoessenFarmProduction.csv";
                txtForecastColTimeIndex.Text = "0";
                txtForecastColValueIndex.Text = "6";
                txtNormalizationValue.Text = "63240";

                forecastUnitType.SelectedIndex = 1;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "13-40";
            }
        }

        /*private void FillGuiWithMeteologica(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
            txtForecastPath.Text = @".\Data\DayAhead\Gwynt\Ml";
            txtObsevationsPath.Text = @".\Data\DayAhead\Gwynt\Observations.csv";
            txtForecastColTimeIndex.Text = "0";
            txtForecastColValueIndex.Text = "1";
            txtNormalizationValue.Text = "192000";

            forecastUnitType.SelectedIndex = 1;
            txtForecastColSeparator.Text = ";";
            txtObservationColSeparator.Text = ";";
            obsevationsUnitType.SelectedIndex = 1;
            txtScope.Text = "0-23";
            }
        }

        private void FillGuiWithAquiloz(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {txtForecastPath.Text = @".\Data\DayAhead\Gwynt\Aqz";
            txtObsevationsPath.Text = @".\Data\DayAhead\Gwynt\Observations.csv";
            txtForecastColTimeIndex.Text = "0";
            txtForecastColValueIndex.Text = "6";
            txtNormalizationValue.Text = "192000";

            forecastUnitType.SelectedIndex = 1;
            txtForecastColSeparator.Text = ";";
            txtObservationColSeparator.Text = ";";
            obsevationsUnitType.SelectedIndex = 1;
            txtScope.Text = "0-23";}
        }*/

        private string siteId = "N/A";
        private void FillGuiWithCamster(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                siteId = "S040";
                txtForecastPath.Text = @".\Data\E.ON\EonOtherPortfolio";
                txtObsevationsPath.Text = @".\Data\E.ON\Camster_Prod.csv";
                txtObservationColValueIndex.Text = "1";
                txtForecastColTimeIndex.Text = "2";
                txtForecastColValueIndex.Text = "3";
                txtNormalizationValue.Text = "50000";

                forecastUnitType.SelectedIndex = 0;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "1-6,12,18,24,30,36,45,48";
            }
        }

        private void FillGuiWithGrimma1(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                siteId = "S014";
                txtForecastPath.Text = @".\Data\E.ON\EonGermanPortfolio";
                txtObsevationsPath.Text = @".\Data\E.ON\Grimma_Prod.csv";
                txtObservationColValueIndex.Text = "1";
                txtForecastColTimeIndex.Text = "2";
                txtForecastColValueIndex.Text = "3";
                txtNormalizationValue.Text = "8000";

                forecastUnitType.SelectedIndex = 0;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "1-6,12,18,24,30,36,45,48";
            }
        }

        private void FillGuiWithGrimma2(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                siteId = "S015";
                txtForecastPath.Text = @".\Data\E.ON\EonGermanPortfolio";
                txtObsevationsPath.Text = @".\Data\E.ON\Grimma_Prod.csv";
                txtObservationColValueIndex.Text = "2";
                txtForecastColTimeIndex.Text = "2";
                txtForecastColValueIndex.Text = "3";
                txtNormalizationValue.Text = "6000";

                forecastUnitType.SelectedIndex = 0;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "1-6,12,18,24,30,36,45,48";
            }
        }

        private void FillGuiWithKarehamn(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                siteId = "S020";
                txtForecastPath.Text = @".\Data\E.ON\EonOtherPortfolio";
                txtObsevationsPath.Text = @".\Data\E.ON\Karehamn_Prod.csv";
                txtObservationColValueIndex.Text = "1";
                txtForecastColTimeIndex.Text = "2";
                txtForecastColValueIndex.Text = "3";
                txtNormalizationValue.Text = "48000";

                forecastUnitType.SelectedIndex = 0;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "1-6,12,18,24,30,36,45,48";
            }
        }

        private void FillGuiWithLondonArray1(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                siteId = "S043";
                txtForecastPath.Text = @".\Data\E.ON\EonOtherPortfolio";
                txtObsevationsPath.Text = @".\Data\E.ON\LA_Prod.csv";
                txtObservationColValueIndex.Text = "1";
                txtForecastColTimeIndex.Text = "2";
                txtForecastColValueIndex.Text = "3";
                txtNormalizationValue.Text = "154800";

                forecastUnitType.SelectedIndex = 0;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "1-6,12,18,24,30,36,45,48";
            }
        }
        
        private void FillGuiWithLondonArray2(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                siteId = "S045";
                txtForecastPath.Text = @".\Data\E.ON\EonOtherPortfolio";
                txtObsevationsPath.Text = @".\Data\E.ON\LA_Prod.csv";
                txtObservationColValueIndex.Text = "2";
                txtForecastColTimeIndex.Text = "2";
                txtForecastColValueIndex.Text = "3";
                txtNormalizationValue.Text = "158400";

                forecastUnitType.SelectedIndex = 0;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "1-6,12,18,24,30,36,45,48";
            }
        }

        private void FillGuiWithLondonArray3(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                siteId = "S046";
                txtForecastPath.Text = @".\Data\E.ON\EonOtherPortfolio";
                txtObsevationsPath.Text = @".\Data\E.ON\LA_Prod.csv";
                txtObservationColValueIndex.Text = "3";
                txtForecastColTimeIndex.Text = "2";
                txtForecastColValueIndex.Text = "3";
                txtNormalizationValue.Text = "158400";

                forecastUnitType.SelectedIndex = 0;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "1-6,12,18,24,30,36,45,48";
            }
        }

        private void FillGuiWithLondonArray4(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                siteId = "S047";
                txtForecastPath.Text = @".\Data\E.ON\EonOtherPortfolio";
                txtObsevationsPath.Text = @".\Data\E.ON\LA_Prod.csv";
                txtObservationColValueIndex.Text = "4";
                txtForecastColTimeIndex.Text = "2";
                txtForecastColValueIndex.Text = "3";
                txtNormalizationValue.Text = "158400";

                forecastUnitType.SelectedIndex = 0;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "1-6,12,18,24,30,36,45,48";
            }
        }

        private void FillGuiWithRobinRiggEast(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                siteId = "S026";
                txtForecastPath.Text = @".\Data\E.ON\EonOtherPortfolio";
                txtObsevationsPath.Text = @".\Data\E.ON\RR_Prod.csv";
                txtObservationColValueIndex.Text = "1";
                txtForecastColTimeIndex.Text = "2";
                txtForecastColValueIndex.Text = "3";
                txtNormalizationValue.Text = "90000";

                forecastUnitType.SelectedIndex = 0;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "1-6,12,18,24,30,36,45,48";
            }
        }

        private void FillGuiWithRobinRiggWest(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                siteId = "S035";
                txtForecastPath.Text = @".\Data\E.ON\EonOtherPortfolio";
                txtObsevationsPath.Text = @".\Data\E.ON\RR_Prod.csv";
                txtObservationColValueIndex.Text = "2";
                txtForecastColTimeIndex.Text = "2";
                txtForecastColValueIndex.Text = "3";
                txtNormalizationValue.Text = "90000";

                forecastUnitType.SelectedIndex = 0;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "1-6,12,18,24,30,36,45,48";
            }
        }

        private void FillGuiWithRoedsand2(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                siteId = "S002";
                txtForecastPath.Text = @".\Data\E.ON\EonOtherPortfolio";
                txtObsevationsPath.Text = @".\Data\E.ON\Roedsand_2_Prod.csv";
                txtObservationColValueIndex.Text = "1";
                txtForecastColTimeIndex.Text = "2";
                txtForecastColValueIndex.Text = "3";
                txtNormalizationValue.Text = "207000";

                forecastUnitType.SelectedIndex = 0;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "1-6,12,18,24,30,36,45,48";
            }
        }

        private void FillGuiWithRoscoe(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                siteId = "S004";
                txtForecastPath.Text = @".\Data\E.ON\EonOtherPortfolio";
                txtObsevationsPath.Text = @".\Data\E.ON\Roscoe_Prod.csv";
                txtObservationColValueIndex.Text = "1";
                txtForecastColTimeIndex.Text = "2";
                txtForecastColValueIndex.Text = "3";
                txtNormalizationValue.Text = "209000";

                forecastUnitType.SelectedIndex = 0;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "1-6,12,18,24,30,36,45,48";
            }
        }

        private void FillGuiWithSerraPelata1(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                siteId = "S008";
                txtForecastPath.Text = @".\Data\E.ON\EonOtherPortfolio";
                txtObsevationsPath.Text = @".\Data\E.ON\Serra_Pelata_1_Prod.csv";
                txtObservationColValueIndex.Text = "1";
                txtForecastColTimeIndex.Text = "2";
                txtForecastColValueIndex.Text = "3";
                txtNormalizationValue.Text = "42000";

                forecastUnitType.SelectedIndex = 0;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "1-6,12,18,24,30,36,45,48";
            }
        }

        private void FillGuiWithSerraPelata2(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                siteId = "S009";
                txtForecastPath.Text = @".\Data\E.ON\EonOtherPortfolio";
                txtObsevationsPath.Text = @".\Data\E.ON\Serra_Pelata_2_Prod.csv";
                txtObservationColValueIndex.Text = "1";
                txtForecastColTimeIndex.Text = "2";
                txtForecastColValueIndex.Text = "3";
                txtNormalizationValue.Text = "12000";

                forecastUnitType.SelectedIndex = 0;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "1-6,12,18,24,30,36,45,48";
            }
        }

        private void FillGuiWithVillkol(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                siteId = "S019";
                txtForecastPath.Text = @".\Data\E.ON\EonOtherPortfolio";
                txtObsevationsPath.Text = @".\Data\E.ON\Villkol_Prod.csv";
                txtObservationColValueIndex.Text = "1";
                txtForecastColTimeIndex.Text = "2";
                txtForecastColValueIndex.Text = "3";
                txtNormalizationValue.Text = "21000";

                forecastUnitType.SelectedIndex = 0;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "1-6,12,18,24,30,36,45,48";
            }
        }

        private void FillGuiWithWildcat(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy)
            {
                siteId = "S018";
                txtForecastPath.Text = @".\Data\E.ON\EonOtherPortfolio";
                txtObsevationsPath.Text = @".\Data\E.ON\Wildcat_1_Prod.csv";
                txtObservationColValueIndex.Text = "1";
                txtForecastColTimeIndex.Text = "2";
                txtForecastColValueIndex.Text = "3";
                txtNormalizationValue.Text = "200000";

                forecastUnitType.SelectedIndex = 0;
                txtForecastColSeparator.Text = ";";
                txtObservationColSeparator.Text = ",";
                obsevationsUnitType.SelectedIndex = 1;
                txtScope.Text = "1-6,12,18,24,30,36,45,48";
            }
        }

        private void CheckBox_IncludeNegProdChecked(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy) includeNegObs = (chbxIncludeNegProd.IsChecked.HasValue) && chbxIncludeNegProd.IsChecked.Value;
        }

        private void CheckBox_FixedHoursChecked(object sender, RoutedEventArgs e)
        {
            if (!UiServices.IsBusy) useFixedHours = (chbxFixedHours.IsChecked.HasValue) && chbxFixedHours.IsChecked.Value;
        }
    }

    /// <summary>
    ///   Contains helper methods for UI, so far just one for showing a waitcursor
    /// </summary>
    public static class UiServices
    {

        /// <summary>
        ///   A value indicating whether the UI is currently busy
        /// </summary>
        public static bool IsBusy;

        /// <summary>
        /// Sets the busystate as busy.
        /// </summary>
        public static void SetBusyState()
        {
            SetBusyState(true);
        }

        /// <summary>
        /// Sets the busystate to busy or not busy.
        /// </summary>
        /// <param name="busy">if set to <c>true</c> the application is now busy.</param>
        public static void SetBusyState(bool busy)
        {
            if (busy != IsBusy)
            {
                IsBusy = busy;
                Mouse.OverrideCursor = busy ? Cursors.Wait : null;

                if (IsBusy)
                {
                    new DispatcherTimer(TimeSpan.FromSeconds(0), DispatcherPriority.ApplicationIdle, dispatcherTimer_Tick, Application.Current.Dispatcher);
                }
            }
        }

        /// <summary>
        /// Handles the Tick event of the dispatcherTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private static void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            var dispatcherTimer = sender as DispatcherTimer;
            if (dispatcherTimer != null)
            {
                if (!IsBusy)
                {
                    SetBusyState(false);
                    dispatcherTimer.Stop();
                }
            }
            else SetBusyState(false);
        }
    }
}
