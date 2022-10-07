using System;
using System.Collections.Generic;

namespace Assets.Scripts.Core.Handlers.DynamicAgentGenerationHandler
{
    public class PoissonEvaluator
    {
        double lambda;

        public PoissonEvaluator(double lambda = 1.0)
        {
            this.lambda = lambda;
        }

        /// <summary>
        /// Returns probability of an int happening during this time period.
        /// </summary>
        public double ProbabilityMassFunction(int k)
        {
            //(l^k / k! ) * e^-l
            //l = lamda
            int kFactorial = Factorial(k);
            double numerator = Math.Pow(Math.E, -lambda) * Math.Pow(lambda, k);

            double p = numerator / kFactorial;
            return p;
        }

        public decimal CummulitiveDistributionFunction(int k)
        {
            double e = Math.Pow(Math.E, (double)-lambda);
            int i = 0;
            double sum = 0.0;
            while (i <= k)
            {
                double n = Math.Pow(lambda, i) / Factorial(i);
                sum += n;
                i++;
            }
            decimal cdf = (decimal)e * (decimal)sum;
            return cdf;
        }

        private int Factorial(int k)
        {
            int count = k;
            int factorial = 1;
            while (count >= 1)
            {
                factorial = factorial * count;
                count--;
            }
            return factorial;
        }

        internal List<double> GenerateList(double cutoff = 0.001)
        {
            var list = new List<double>();

            double probability = 1f;
            int i = 0;

            while (probability > cutoff && i < 3000)
            {
                probability = ProbabilityMassFunction(i);
                list.Add(probability);
                i++;
            }

            return list;
        }
    }
}
