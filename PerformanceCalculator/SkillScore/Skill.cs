using System;
using System.Collections.Generic;
using System.Linq;

namespace PerformanceCalculator
{
    public class Skill
    {
        public readonly double MeanForecastingError;
        public readonly double MeanAbsoluteError;
        public readonly double MeanSquaredError;
        public readonly double RootMeanSquaredError;
        public readonly double MeanAbsolutePercentageError;
        public readonly double RootMeanSquaredPrecentageError;

        public Skill(double meanForecastingError, double meanAbsoluteError, double meanSquaredError, double rootMeanSquaredError, double meanAbsolutePercentageError, double rootMeanSquaredPrecentageError)
        {
            MeanForecastingError = meanForecastingError;
            MeanAbsoluteError = meanAbsoluteError;
            MeanSquaredError = meanSquaredError;
            RootMeanSquaredError = rootMeanSquaredError;
            MeanAbsolutePercentageError = meanAbsolutePercentageError;
            RootMeanSquaredPrecentageError = rootMeanSquaredPrecentageError;
        }

        public static Skill Average(IEnumerable<Skill> skills)
        {

            return new Skill(
                skills.Average(s => s.MeanForecastingError),
                skills.Average(s => s.MeanAbsoluteError),
                skills.Average(s => s.MeanSquaredError),
                skills.Average(s => s.RootMeanSquaredError),
                skills.Average(s => s.MeanAbsolutePercentageError),
                skills.Average(s => s.RootMeanSquaredPrecentageError)
                );
        }

        public double Get(string name)
        {
            if (name == "MFE") return MeanForecastingError;
            if (name == "MAE") return MeanAbsoluteError;
            if (name == "MAPE") return MeanAbsolutePercentageError;
            if (name == "MSE") return MeanSquaredError;
            if (name == "RMSE") return RootMeanSquaredError;
            if (name == "RMSPE") return RootMeanSquaredPrecentageError;

            throw new Exception("Unkown statistical method: " + name);
        }
    }
}