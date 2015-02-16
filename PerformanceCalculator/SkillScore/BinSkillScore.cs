using System;

namespace PerformanceCalculator.SkillScore
{
    public class BinSkillScore
    {
        public readonly int SkillId;
        public readonly string SkillIdLabel;
        public readonly int InstalledCapacity;

        public double SumDiff { get; private set; }
        public double SumAbsDiff { get; private set; }
        public double SumSqDiff { get; private set; }
        public double SumResidualError { get; private set; }
        public double SumAbsResidualError { get; private set; }
        public double SumSqResidualError { get; private set; }

        public int NrOfOccurrences { get; private set; }

        /// <summary>
        /// A Bin calculating statistics about forecastes and measured values.
        /// </summary>
        /// <param name="skillId">The id of the bin</param>
        /// <param name="skillIdLabel">A label to be printed for the bin.</param>
        /// <param name="installedCapacity">
        /// Capacity for normalizing the comparing values, if no value is assigned, 
        /// capacity will not be used and residuals will be calculated agaist measured values.
        /// </param>
        public BinSkillScore(int skillId, string skillIdLabel, int installedCapacity = 0)
        {
            SkillId = skillId;
            SkillIdLabel = skillIdLabel;
            InstalledCapacity = installedCapacity;
            NrOfOccurrences = 0;
        }

        public void Register(double actual, double forecast)
        {
            NrOfOccurrences++;

            double diff = actual - forecast;
            double absDiff = Math.Abs(diff);
            double sqDiff = diff * diff;

            double normalizedDiff;
            if (InstalledCapacity > 0) normalizedDiff = diff/InstalledCapacity;
            else
            {   // Normalizaion against observed should never happen. All normalization should happen against a fixed number
                // or some observed average over a period. Otherwise it is no normalization.
                if (Math.Abs(actual) < 0.00) return;  // This will possibly end in divide by zero, or a very small number will impact the end result a lot. Skip observation.
                normalizedDiff = diff / actual;
            }

            double absNormalizedDiff = Math.Abs(normalizedDiff);
            double sqResidual = normalizedDiff * normalizedDiff;

            SumDiff += diff;
            SumAbsDiff += absDiff;
            SumSqDiff += sqDiff;
            SumResidualError += normalizedDiff;
            SumAbsResidualError += absNormalizedDiff;
            SumSqResidualError += sqResidual;
        }

        public double MeanForecastingError
        {
            get { return SumDiff / NrOfOccurrences; }
        }

        public double MeanAbsoluteError
        {
            get { return SumAbsDiff / NrOfOccurrences; }
        }

        public double MeanSquaredError
        {
            get { return SumSqDiff / NrOfOccurrences; }
        }

        public double RootMeanSquaredError
        {
            get { return Math.Sqrt(SumSqDiff / NrOfOccurrences); }
        }

        public double MeanAbsolutePercentageError
        {
            get { return (SumAbsResidualError / NrOfOccurrences) * 100; }
        }

        public double RootMeanSquaredPrecentageError
        {
            get { return Math.Sqrt(SumSqResidualError / NrOfOccurrences) * 100; }
        }

        public override string ToString()
        {
            string msg = "{0}:\t MFE:{1}\t MAE:{2}\t MAPE:{3}\t MSE:{4}\t RMSE:{5}\t RMSPE:{6}";
            return string.Format(msg, SkillIdLabel, FormatDouble(MeanForecastingError), FormatDouble(MeanAbsoluteError), FormatDouble(MeanAbsolutePercentageError), FormatDouble(MeanSquaredError), FormatDouble(RootMeanSquaredError), FormatDouble(RootMeanSquaredPrecentageError));
        }

        public string Print(string statMode, bool useSkillIdLabel = true)
        {
            if (statMode == "MFE") return Print(true, false, false, false, false, false, useSkillIdLabel);
            if (statMode == "MAE") return Print(false, true, false, false, false, false, useSkillIdLabel);
            if (statMode == "MAPE") return Print(false, false, true, false, false, false, useSkillIdLabel);
            if (statMode == "MSE") return Print(false, false, false, true, false, false, useSkillIdLabel);
            if (statMode == "RMSE") return Print(false, false, false, false, true, false, useSkillIdLabel);
            if (statMode == "RMSPE") return Print(false, false, false, false, false, true, useSkillIdLabel);

            throw new Exception("Unkown statistical method: "+statMode);
        }

        public string Print(bool mfe = true, bool mae = true, bool mape = true, bool mse = true, bool rmse = true, bool rmspe = true, bool useSkillIdLabel = true)
        {
            string msg = string.Empty;
            if (useSkillIdLabel) msg += SkillIdLabel;
            if (mfe) msg += "\t MFE \t" + FormatDouble(MeanForecastingError);
            if (mae) msg += "\t MAE \t" + FormatDouble(MeanAbsoluteError);
            if (mape) msg += "\t MAPE \t" + FormatDouble(MeanAbsolutePercentageError);
            if (mse) msg += "\t MSE \t" + FormatDouble(MeanSquaredError);
            if (rmse) msg += "\t RMSE \t" + FormatDouble(RootMeanSquaredError);
            if (rmspe) msg += "\t RMSPE \t" + FormatDouble(RootMeanSquaredPrecentageError);

            return msg;
        }

        public static string FormatDouble(double value)
        {
            string val = string.Empty;
            val = String.Format("{0,10:0.0000}", value);
            return val;
        }

        public void Merge(BinSkillScore skillScore)
        {
            SumDiff += skillScore.SumDiff;
            SumAbsDiff += skillScore.SumAbsDiff;
            SumSqDiff += skillScore.SumSqDiff;
            SumResidualError += skillScore.SumResidualError;
            SumAbsResidualError += skillScore.SumAbsResidualError;
            SumSqResidualError += skillScore.SumSqResidualError;
            NrOfOccurrences += skillScore.NrOfOccurrences;
        }

        public Skill GetSkill()
        {
            return new Skill(MeanForecastingError, MeanAbsoluteError, MeanSquaredError, RootMeanSquaredError, MeanAbsolutePercentageError, RootMeanSquaredPrecentageError);
        }

        public string AsCsvSeperatedLine(char seperator)
        {
            var sep = seperator;
            string msg = "{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}";
            return string.Format(msg, FormatDouble(MeanForecastingError), sep, FormatDouble(MeanAbsoluteError), sep, FormatDouble(MeanAbsolutePercentageError), sep, FormatDouble(MeanSquaredError), sep, FormatDouble(RootMeanSquaredError), sep, FormatDouble(RootMeanSquaredPrecentageError));
        }
    }
}