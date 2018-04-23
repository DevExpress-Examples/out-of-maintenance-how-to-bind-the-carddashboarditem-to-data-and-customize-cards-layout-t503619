using DevExpress.DashboardCommon;

namespace CustomCardsLayout
{
    public partial class Form1 : DevExpress.XtraBars.Ribbon.RibbonForm
    {
        public Form1()
        {
            InitializeComponent();
            dashboardDesigner1.CreateRibbon();

            Dashboard dashboard = new Dashboard();
            DashboardObjectDataSource salesDataSource = CreateDataSource();
            dashboard.DataSources.Add(salesDataSource);

            CardDashboardItem cardItem = new CardDashboardItem();
            CreateCards(cardItem, salesDataSource);
            dashboard.Items.Add(cardItem);
            dashboardDesigner1.Dashboard = dashboard;
        }

        private void CreateCards(CardDashboardItem cardItem, DashboardObjectDataSource salesDataSource)
        {
            cardItem.DataSource = salesDataSource;
            Card card = new Card();
            card.ActualValue = new Measure("Sales", SummaryType.Sum);
            card.TargetValue = new Measure("SalesTarget", SummaryType.Sum);
            cardItem.SeriesDimensions.Add(new Dimension("State"));
            cardItem.SparklineArgument = new Dimension("CurrentDate", DateTimeGroupInterval.MonthYear);

            CardCustomLayoutTemplate customTemplate = new CardCustomLayoutTemplate();
            CardRow row1 = new CardRow();
            CardRowDataElement dimensionValue_row1 = new CardRowDataElement(CardRowDataElementType.DimensionValue, 0, 
                CardHorizontalAlignment.Left, 14, System.Drawing.Color.DarkBlue);
            CardRowIndicatorElement deltaIndicator_row1 = new CardRowIndicatorElement(CardHorizontalAlignment.Right, 22);
            row1.Elements.AddRange(dimensionValue_row1, deltaIndicator_row1);

            CardRow row2 = new CardRow();
            CardRowTextElement staticText_row2 = new CardRowTextElement("Sales: ", CardHorizontalAlignment.Left, 12, 
                CardPredefinedColor.Neutral);
            CardRowDataElement salesValue_row2 = new CardRowDataElement(CardRowDataElementType.ActualValue, 
                CardHorizontalAlignment.Left, 12, CardPredefinedColor.Main);
            row2.Elements.AddRange(staticText_row2, salesValue_row2);

            CardRow row3 = new CardRow();
            CardRowTextElement staticText_row3 = new CardRowTextElement("Target: ", CardHorizontalAlignment.Left, 12, 
                CardPredefinedColor.Neutral);
            CardRowDataElement salesValue_row3 = new CardRowDataElement(CardRowDataElementType.TargetValue, 
                CardHorizontalAlignment.Left, 12, CardPredefinedColor.Main);
            row3.Indent = 30;
            row3.Elements.AddRange(staticText_row3, salesValue_row3);

            CardSparklineRow row4 = new CardSparklineRow();
            row4.VerticalAlignment = CardVerticalAlignment.Bottom;

            customTemplate.Layout = new CardLayout();
            customTemplate.Layout.Rows.AddRange(row1, row2, row3, row4);
            card.LayoutTemplate = customTemplate;
            cardItem.Cards.Add(card);            
        }

        private DashboardObjectDataSource CreateDataSource()
        {
            DashboardObjectDataSource salesDataSource = new DashboardObjectDataSource();
            salesDataSource.DataSource = typeof(SalesOverviewDataGenerator);
            salesDataSource.DataMember = "GetData";
            salesDataSource.Fill();
            return salesDataSource;
        }
    }
}
