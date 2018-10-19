//Copyright (c) 2018 Giulio Occhionero

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using static System.DateTime;

namespace Finance
{
    /// <summary>
    /// The type of option to be assigned in the constructor.
    /// </summary>
    public enum OptionType { Call, Put }

    /// <summary>
    /// An option class enforcing both standardized expiration dates and strike intervals.
    /// </summary>
    public class Option
    {
        /// <summary>
        /// Static constructor needed to initially compute the full past and future expiration calendar.
        /// </summary>
        static Option()
        {
            ComputeExpirations();
        }

        /// <summary>
        /// Instance constructor of an option object.
        /// </summary>
        /// <param name="underlying">The underlying stock symbol, needed for interfacing with quote providers and analytics.</param>
        /// <param name="date">The date in time the option has supposedly been created.</param>
        /// <param name="months">The forward months to the desired expiration: zero will return the first available future expiration date.</param>
        /// <param name="type">The type of the option.</param>
        /// <param name="strikeorprice">Either the underlying stock price or the strike price. The constructor will default to the closest valid strike price.</param>
        public Option(string underlying, DateTime date, int months, OptionType type, double strikeorprice)
        {
            Underlying = underlying.ToUpper();
            Expiration = GetExpiration(date, months);
            Type = type;
            Strike = ClosestStrike(strikeorprice);
        }

        /// <summary>
        /// A list containing both historical and future expiration dates.
        /// </summary>
        public static List<DateTime> Expirations { get; private set; } = new List<DateTime>();

        /// <summary>
        /// The underlying stock symbol.
        /// </summary>
        public string Underlying { get; }

        /// <summary>
        /// The expiration date.
        /// </summary>
        public DateTime Expiration { get; }

        /// <summary>
        /// The option type.
        /// </summary>
        public OptionType @Type { get; }

        /// <summary>
        /// The strike price.
        /// </summary>
        public double Strike { get; }

        /// <summary>
        /// Function to get the closest valid strike price for an underlying stock price; also used in at-the-money and similar strategies.
        /// </summary>
        /// <param name="price">Underlying stock price.</param>
        /// <returns>Returns the nearest valid strike.</returns>
        public static double ClosestStrike(double price)
        {
            double sp = StrikeSpacing(price);
            return Math.Round(price / sp) * sp;
        }

        /// <summary>
        /// Initially builds the past and forward expirations list based on the required intervals.
        /// </summary>
        private static void ComputeExpirations(int pastyears = 10, int futureyears = 1)
        {
            Expirations.Clear();
            DateTime date = new DateTime(Now.Year, Now.Month, 1, 23, 59, 59).AddYears(-pastyears);
            DateTime stop = new DateTime(Now.Year, Now.Month, 1, 23, 59, 59).AddYears(futureyears).AddMonths(1);
            int fridays = 0;
            while (date < stop)
            {
                if (date.Day == 1) fridays = 0;
                while (fridays < 3)
                {
                    if (date.DayOfWeek == DayOfWeek.Friday) fridays += 1;
                    if (fridays == 3) Expirations.Add(date);
                    date = date.AddDays(1);
                }
                date = new DateTime(date.Year, date.Month, 1, 23, 59, 59).AddMonths(1);
            }
        }

        /// <summary>
        /// An auxiliary function to get expiration dates relative to a certain date and selected by future months.
        /// </summary>
        /// <param name="date">The date relative to which go into future months in order to get the desired expiration date.</param>
        /// <param name="forwardmonths">The months forward starting from the specified date to get the expiration date. Zero returns the next available future expiration date.</param>
        /// <returns>Returns the date of the desired expiration.</returns>
        public static DateTime GetExpiration(DateTime date, int forwardmonths = 0) => Expirations.Where(e => e > date).ToArray()[forwardmonths];

        /// <summary>
        /// An auxiliary function to quickly produce a spectrum of options scattered around an underlying price.
        /// </summary>
        /// <param name="underlying">Underlying stock symbol.</param>
        /// <param name="date">The date in time the option has supposedly been created.</param>
        /// <param name="months">The months forward to get the expiration. Zero returns the next expiration in the future.</param>
        /// <param name="price">Underlying stock price.</param>
        /// <param name="type">The type of the option.</param>
        /// <param name="levels">The count of how many strikes, above and below the closest strike, to include in the chain.</param>
        /// <returns>Returns a collection of options.</returns>
        public static IEnumerable<Option> OptionChain(string underlying, DateTime date, int months, double price, OptionType type, int levels = 4)
        {
            List<double> sc = StrikeChain(price, levels);
            return sc.Select(k => new Option(underlying, date, months, type, k));
        }

        /// <summary>
        /// An auxiliary function to quickly produce a spectrum of strikes scattered around an underlying price.
        /// </summary>
        /// <param name="price">Underlying stock price.</param>
        /// <param name="levels">The count of how many strikes, above and below the closest strike, to include in the chain.</param>
        /// <returns>Returns a collection of strikes.</returns>
        public static List<double> StrikeChain(double price, int levels = 4)
        {
            double sp = StrikeSpacing(price);
            double cs = ClosestStrike(price);
            List<double> sc = new List<double> { cs };
            for (int i = 1; i <= levels; i++)
            {
                double low = cs - i * sp;
                if (low > 0.0) sc.Insert(0, low);
                double high = cs + i * sp;
                sc.Add(high);
            }
            return sc;
        }

        /// <summary>
        /// An auxiliary function to produce the valid strike spacing given an underlying stock price.
        /// </summary>
        /// <param name="price">Underlying stock price.</param>
        /// <returns>Returns the strike interval for the given underlying price.</returns>
        private static double StrikeSpacing(double price)
        {
            if (price <= 25.0) return 2.5;
            else if (price > 25.0 && price <= 200.0) return 5.0;
            else return 10.0;
        }

        /// <summary>
        /// Function to calculate the option symbol given its properties.
        /// </summary>
        /// <returns>Returns the option symbol.</returns>
        public override string ToString()
        {
            return Underlying + $"{Expiration:yyMMdd}" + Type.ToString().Substring(0, 1) + $"{Strike:00000.000}".Replace(",", "").Replace(".", "");
        }
    }
}
