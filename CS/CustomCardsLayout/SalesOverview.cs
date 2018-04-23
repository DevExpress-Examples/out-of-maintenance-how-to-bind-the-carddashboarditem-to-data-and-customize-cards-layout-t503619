using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace CustomCardsLayout
{
    public class SalesOverviewDataGenerator : SalesDataGenerator {
        public class DataItem {
            decimal sTarget;
            decimal sal;
            DateTime curtDate;
            string cat;
            string st;

            public string State {
                get { return st; }
                set { st = value; }
            }
            public string Category {
                get { return cat; }
                set { cat = value; }
            }
            public DateTime CurrentDate {
                get { return curtDate; }
                set { curtDate = value; }
            }
            public decimal Sales {
                get { return sal; }
                set { sal = value; }
            }
            public decimal SalesTarget {
                get { return sTarget; }
                set { sTarget = value; }
            }
        }

        public class DataKey {
            readonly string state;
            readonly string category;
            readonly DateTime dt;

            public DataKey(string state, string category, DateTime dt) {
                this.state = state;
                this.category = category;
                this.dt = dt;
            }
            public override bool Equals(object obj) {
                DataKey key = (DataKey)obj;
                return key.state == state && key.category == category && key.dt == dt;
            }
            public override int GetHashCode() {
                return state.GetHashCode() ^ category.GetHashCode() ^ dt.GetHashCode();
            }
        }

        readonly Dictionary<DataKey, DataItem> dat = new Dictionary<DataKey, DataItem>();
        readonly DateTime startDate;
        readonly DateTime endDate;

        public IEnumerable<DataItem> Data { get { return dat.Values; } }

        public SalesOverviewDataGenerator(DataSet dataSet)
            : base(dataSet) {
            endDate = DateTime.Today;
            startDate = new DateTime(endDate.Year - 2, 1, 1);
        }
        protected override void Generate(Context context) {
            DateTime dt = startDate;
            while(dt < endDate) {
                if(dt.DayOfWeek == DayOfWeek.Monday) {
                    context.UnitsSoldGenerator.Next();
                    decimal sales = context.UnitsSoldGenerator.UnitsSold * context.ListPrice;
                    decimal salesTarget = context.UnitsSoldGenerator.UnitsSoldTarget * context.ListPrice;
                    DataKey datKey = new DataKey(context.State, context.CategoryName, dt);
                    DataItem datItem = null;
                    if(!dat.TryGetValue(datKey, out datItem)) {
                        datItem = new DataItem {
                            CurrentDate = dt,
                            Category = context.CategoryName,
                            State = context.State,
                        };
                        dat.Add(datKey, datItem);
                    }
                    datItem.Sales += sales;
                    datItem.SalesTarget += salesTarget;
                }
                dt = dt.AddDays(1);
            }
        }

        public static IEnumerable<SalesOverviewDataGenerator.DataItem> GetData()
        {     
            DataSet ds = new DataSet();
            ds.ReadXml(@"..\..\DashboardSales.xml", XmlReadMode.ReadSchema);
            SalesOverviewDataGenerator dataGenerator = new SalesOverviewDataGenerator(ds);
            dataGenerator.Generate();
            return dataGenerator.Data;
        }
    }

    public abstract class SalesDataGenerator
    {
        public class Context
        {
            readonly string st;
            readonly string prodName;
            readonly string catName;
            readonly decimal lPrice;
            readonly UnitsSoldRandomGenerator uSoldGenerator;

            public string State { get { return st; } }
            public string ProductName { get { return prodName; } }
            public string CategoryName { get { return catName; } }
            public decimal ListPrice { get { return lPrice; } }
            public UnitsSoldRandomGenerator UnitsSoldGenerator { get { return uSoldGenerator; } }

            public Context(string st, string prodName, string catName, decimal lPrice, UnitsSoldRandomGenerator uSoldGenerator)
            {
                this.st = st;
                this.prodName = prodName;
                this.catName = catName;
                this.lPrice = lPrice;
                this.uSoldGenerator = uSoldGenerator;
            }
        }

        protected static string GetState(DataRow region)
        {
            return (string)region["Region"];
        }
        protected static string GetProductName(DataRow product)
        {
            return (string)product["Name"];
        }
        protected static decimal GetListPrice(DataRow product)
        {
            return (decimal)product["ListPrice"];
        }

        readonly DataSet ds;
        readonly DataTable categoriesTable;
        readonly DataTable productsTable;
        readonly DataTable regionsTable;
        readonly Random rand = new Random(1);
        readonly ProductClasses prodClasses;
        readonly RegionClasses regClasses;

        protected DataRowCollection Regions { get { return regionsTable.Rows; } }
        protected DataRowCollection Products { get { return productsTable.Rows; } }
        protected ProductClasses ProdClasses { get { return prodClasses; } }
        protected RegionClasses RegClasses { get { return regClasses; } }
        protected Random Random { get { return rand; } }

        protected SalesDataGenerator(DataSet ds)
        {
            this.ds = ds;
            categoriesTable = ds.Tables["Categories"];
            productsTable = ds.Tables["Products"];
            regionsTable = ds.Tables["Regions"];
            prodClasses = new ProductClasses(productsTable.Rows);
            regClasses = new RegionClasses(regionsTable.Rows);
        }
        protected double GetRegionWeigtht(DataRow region)
        {
            return regClasses[(int)region["RegionID"]];
        }
        protected ProductClass GetProductClass(DataRow product)
        {
            return prodClasses[(int)product["ProductID"]];
        }
        protected string GetCategoryName(DataRow product)
        {
            return (string)categoriesTable.Select(string.Format("CategoryID = {0}", product["CategoryID"]))[0]["CategoryName"];
        }
        protected UnitsSoldRandomGenerator CreateUnitsSoldGenerator(double regionWeight, ProductClass productClass)
        {
            return new UnitsSoldRandomGenerator(rand, (int)Math.Ceiling(productClass.SaleProbability * regionWeight));
        }
        protected abstract void Generate(Context context);
        protected virtual void EndGenerate()
        {
        }
        public void Generate()
        {
            foreach (DataRow region in Regions)
            {
                string state = GetState(region);
                double regionWeight = GetRegionWeigtht(region);
                foreach (DataRow product in Products)
                {
                    UnitsSoldRandomGenerator unitsSoldgenerator = CreateUnitsSoldGenerator(regionWeight, GetProductClass(product));
                    Generate(new Context(state, GetProductName(product), GetCategoryName(product), GetListPrice(product), unitsSoldgenerator));
                }
            }
            EndGenerate();
        }
    }

    public class UnitsSoldRandomGenerator
    {
        const int MinUnitsSold = 5;

        readonly Random rand;
        readonly int startUnitsSold;
        int? prevUnitsSold;
        int? prevPrevUnitsSold;
        int unitsSold;
        int unitsSoldTarget;
        bool isFirst = true;

        public int UnitsSold { get { return unitsSold; } }
        public int UnitsSoldTarget { get { return unitsSoldTarget; } }

        public UnitsSoldRandomGenerator(Random rand, int startUnitsSold)
        {
            this.rand = rand;
            this.startUnitsSold = Math.Max(startUnitsSold, MinUnitsSold);
        }
        public void Next()
        {
            if (isFirst)
            {
                unitsSold = startUnitsSold;
                isFirst = false;
            }
            else
            {
                unitsSold = unitsSold + (int)Math.Round(DataHelper.Random(rand, unitsSold * 0.5));
                unitsSold = Math.Max(unitsSold, MinUnitsSold);
            }
            int unitsSoldSum = unitsSold;
            int count = 1;
            if (prevUnitsSold.HasValue)
            {
                unitsSoldSum += prevUnitsSold.Value;
                count++;
            }
            if (prevPrevUnitsSold.HasValue)
            {
                unitsSoldSum += prevPrevUnitsSold.Value;
                count++;
            }
            unitsSoldTarget = (int)Math.Round((double)unitsSoldSum / count);
            unitsSoldTarget = unitsSoldTarget + (int)Math.Round(DataHelper.Random(rand, unitsSoldTarget));
            prevPrevUnitsSold = prevUnitsSold;
            prevUnitsSold = unitsSold;
        }
    }

    public class ProductClasses : List<ProductClass>
    {
        public new ProductClass this[int productID]
        {
            get
            {
                foreach (ProductClass productClass in this)
                    if (productClass.ContainsProduct(productID))
                        return productClass;
                throw new ArgumentException("procutID");
            }
        }

        public ProductClasses(ICollection products)
        {
            Add(new ProductClass(null, 100m, 0.5));
            Add(new ProductClass(100m, 500m, 0.4));
            Add(new ProductClass(500m, 1500m, 0.3));
            Add(new ProductClass(1500m, null, 0.2));
            foreach (DataRow product in products)
            {
                int productID = (int)product["ProductID"];
                decimal listPrice = (decimal)product["ListPrice"];
                foreach (ProductClass productClass in this)
                    if (productClass.AddProduct(productID, listPrice))
                        break;
            }
        }
    }

    public class ProductClass
    {
        readonly List<int> productIDs = new List<int>();
        readonly decimal? minPrice;
        readonly decimal? maxPrice;
        readonly double saleProbability;

        public double SaleProbability { get { return saleProbability; } }

        public ProductClass(decimal? minPrice, decimal? maxPrice, double saleProbability)
        {
            this.minPrice = minPrice;
            this.maxPrice = maxPrice;
            this.saleProbability = saleProbability;
        }
        public bool AddProduct(int productID, decimal price)
        {
            bool satisfyMinPrice = !minPrice.HasValue || price >= minPrice.Value;
            bool satisfyMaxPrice = !maxPrice.HasValue || price < maxPrice.Value;
            if (satisfyMinPrice && satisfyMaxPrice)
            {
                productIDs.Add(productID);
                return true;
            }
            return false;
        }
        public bool ContainsProduct(int productID)
        {
            return productIDs.Contains(productID);
        }
    }

    public class RegionClasses : Dictionary<int, double>
    {
        public RegionClasses(ICollection regions)
        {
            int? numberEmployeesMin = null;
            foreach (DataRow region in regions)
            {
                short numberEmployees = (short)region["NumberEmployees"];
                numberEmployeesMin = numberEmployeesMin.HasValue ? Math.Min(numberEmployeesMin.Value, numberEmployees) : numberEmployees;
            }
            foreach (DataRow region in regions)
                Add((int)region["RegionID"], (short)region["NumberEmployees"] / (double)numberEmployeesMin.Value);
        }
    }

    public static class DataHelper
    {
        public static double Random(Random random, double deviation, bool positive)
        {
            int rand = random.Next(positive ? 0 : -1000000, 1000000);
            return (double)rand / 1000000 * deviation;
        }
        public static double Random(Random random, double deviation)
        {
            return Random(random, deviation, false);
        }
    }
}
