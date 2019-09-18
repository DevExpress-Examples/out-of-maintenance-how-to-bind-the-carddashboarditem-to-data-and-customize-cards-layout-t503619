Imports DevExpress.DashboardCommon

Namespace CustomCardsLayout
	Partial Public Class Form1
		Inherits DevExpress.XtraBars.Ribbon.RibbonForm

		Public Sub New()
			InitializeComponent()
			dashboardDesigner1.DataSourceOptions.ObjectDataSourceLoadingBehavior = DevExpress.DataAccess.DocumentLoadingBehavior.LoadAsIs
			dashboardDesigner1.CreateRibbon()

			Dim dashboard As New Dashboard()
			Dim salesDataSource As DashboardObjectDataSource = CreateDataSource()
			dashboard.DataSources.Add(salesDataSource)

			Dim cardItem As New CardDashboardItem()
			CreateCards(cardItem, salesDataSource)
			dashboard.Items.Add(cardItem)
			dashboardDesigner1.Dashboard = dashboard
		End Sub

		Private Sub CreateCards(ByVal cardItem As CardDashboardItem, ByVal salesDataSource As DashboardObjectDataSource)
			cardItem.DataSource = salesDataSource
			Dim card As New Card()
			card.ActualValue = New Measure("Sales", SummaryType.Sum)
			card.TargetValue = New Measure("SalesTarget", SummaryType.Sum)
			cardItem.SeriesDimensions.Add(New Dimension("State"))
			cardItem.SparklineArgument = New Dimension("CurrentDate", DateTimeGroupInterval.MonthYear)

			Dim customTemplate As New CardCustomLayoutTemplate()
			Dim row1 As New CardRow()
			Dim dimensionValue_row1 As New CardRowDataElement(CardRowDataElementType.DimensionValue, 0, CardHorizontalAlignment.Left, 14, System.Drawing.Color.DarkBlue)
			Dim deltaIndicator_row1 As New CardRowIndicatorElement(CardHorizontalAlignment.Right, 22)
			row1.Elements.AddRange(dimensionValue_row1, deltaIndicator_row1)

			Dim row2 As New CardRow()
			Dim staticText_row2 As New CardRowTextElement("Sales: ", CardHorizontalAlignment.Left, 12, CardPredefinedColor.Neutral)
			Dim salesValue_row2 As New CardRowDataElement(CardRowDataElementType.ActualValue, CardHorizontalAlignment.Left, 12, CardPredefinedColor.Main)
			row2.Elements.AddRange(staticText_row2, salesValue_row2)

			Dim row3 As New CardRow()
			Dim staticText_row3 As New CardRowTextElement("Target: ", CardHorizontalAlignment.Left, 12, CardPredefinedColor.Neutral)
			Dim salesValue_row3 As New CardRowDataElement(CardRowDataElementType.TargetValue, CardHorizontalAlignment.Left, 12, CardPredefinedColor.Main)
			row3.Indent = 30
			row3.Elements.AddRange(staticText_row3, salesValue_row3)

			Dim row4 As New CardSparklineRow()
			row4.VerticalAlignment = CardVerticalAlignment.Bottom

			customTemplate.Layout = New CardLayout()
			customTemplate.Layout.Rows.AddRange(row1, row2, row3, row4)
			card.LayoutTemplate = customTemplate
			cardItem.Cards.Add(card)
		End Sub

		Private Function CreateDataSource() As DashboardObjectDataSource
			Dim salesDataSource As New DashboardObjectDataSource()
			salesDataSource.DataSource = GetType(SalesOverviewDataGenerator)
			salesDataSource.DataMember = "GetData"
			salesDataSource.Fill()
			Return salesDataSource
		End Function
	End Class
End Namespace
