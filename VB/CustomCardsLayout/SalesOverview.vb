Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Data

Namespace CustomCardsLayout
	Public Class SalesOverviewDataGenerator
		Inherits SalesDataGenerator

		Public Class DataItem
			Private sTarget As Decimal
			Private sal As Decimal
			Private curtDate As Date
			Private cat As String
			Private st As String

			Public Property State() As String
				Get
					Return st
				End Get
				Set(ByVal value As String)
					st = value
				End Set
			End Property
			Public Property Category() As String
				Get
					Return cat
				End Get
				Set(ByVal value As String)
					cat = value
				End Set
			End Property
			Public Property CurrentDate() As Date
				Get
					Return curtDate
				End Get
				Set(ByVal value As Date)
					curtDate = value
				End Set
			End Property
			Public Property Sales() As Decimal
				Get
					Return sal
				End Get
				Set(ByVal value As Decimal)
					sal = value
				End Set
			End Property
			Public Property SalesTarget() As Decimal
				Get
					Return sTarget
				End Get
				Set(ByVal value As Decimal)
					sTarget = value
				End Set
			End Property
		End Class

		Public Class DataKey
			Private ReadOnly state As String
			Private ReadOnly category As String
			Private ReadOnly dt As Date

			Public Sub New(ByVal state As String, ByVal category As String, ByVal dt As Date)
				Me.state = state
				Me.category = category
				Me.dt = dt
			End Sub
			Public Overrides Function Equals(ByVal obj As Object) As Boolean
				Dim key As DataKey = DirectCast(obj, DataKey)
				Return key.state = state AndAlso key.category = category AndAlso key.dt = dt
			End Function
			Public Overrides Function GetHashCode() As Integer
				Return state.GetHashCode() Xor category.GetHashCode() Xor dt.GetHashCode()
			End Function
		End Class

		Private ReadOnly dat As New Dictionary(Of DataKey, DataItem)()
		Private ReadOnly startDate As Date
		Private ReadOnly endDate As Date

		Public ReadOnly Property Data() As IEnumerable(Of DataItem)
			Get
				Return dat.Values
			End Get
		End Property

		Public Sub New(ByVal dataSet As DataSet)
			MyBase.New(dataSet)
			endDate = Date.Today
			startDate = New Date(endDate.Year - 2, 1, 1)
		End Sub
		Protected Overrides Sub Generate(ByVal context As Context)
			Dim dt As Date = startDate
			Do While dt < endDate
				If dt.DayOfWeek = DayOfWeek.Monday Then
					context.UnitsSoldGenerator.Next()
					Dim sales As Decimal = context.UnitsSoldGenerator.UnitsSold * context.ListPrice
					Dim salesTarget As Decimal = context.UnitsSoldGenerator.UnitsSoldTarget * context.ListPrice
					Dim datKey As New DataKey(context.State, context.CategoryName, dt)
					Dim datItem As DataItem = Nothing
					If Not dat.TryGetValue(datKey, datItem) Then
						datItem = New DataItem With {.CurrentDate = dt, .Category = context.CategoryName, .State = context.State}
						dat.Add(datKey, datItem)
					End If
					datItem.Sales += sales
					datItem.SalesTarget += salesTarget
				End If
				dt = dt.AddDays(1)
			Loop
		End Sub

		Public Shared Function GetData() As IEnumerable(Of SalesOverviewDataGenerator.DataItem)
			Dim ds As New DataSet()
			ds.ReadXml("..\..\DashboardSales.xml", XmlReadMode.ReadSchema)
			Dim dataGenerator As New SalesOverviewDataGenerator(ds)
			dataGenerator.Generate()
			Return dataGenerator.Data
		End Function
	End Class

	Public MustInherit Class SalesDataGenerator
		Public Class Context
			Private ReadOnly st As String
			Private ReadOnly prodName As String
			Private ReadOnly catName As String
			Private ReadOnly lPrice As Decimal
			Private ReadOnly uSoldGenerator As UnitsSoldRandomGenerator

			Public ReadOnly Property State() As String
				Get
					Return st
				End Get
			End Property
			Public ReadOnly Property ProductName() As String
				Get
					Return prodName
				End Get
			End Property
			Public ReadOnly Property CategoryName() As String
				Get
					Return catName
				End Get
			End Property
			Public ReadOnly Property ListPrice() As Decimal
				Get
					Return lPrice
				End Get
			End Property
			Public ReadOnly Property UnitsSoldGenerator() As UnitsSoldRandomGenerator
				Get
					Return uSoldGenerator
				End Get
			End Property

			Public Sub New(ByVal st As String, ByVal prodName As String, ByVal catName As String, ByVal lPrice As Decimal, ByVal uSoldGenerator As UnitsSoldRandomGenerator)
				Me.st = st
				Me.prodName = prodName
				Me.catName = catName
				Me.lPrice = lPrice
				Me.uSoldGenerator = uSoldGenerator
			End Sub
		End Class

		Protected Shared Function GetState(ByVal region As DataRow) As String
			Return DirectCast(region("Region"), String)
		End Function
		Protected Shared Function GetProductName(ByVal product As DataRow) As String
			Return DirectCast(product("Name"), String)
		End Function
		Protected Shared Function GetListPrice(ByVal product As DataRow) As Decimal
			Return DirectCast(product("ListPrice"), Decimal)
		End Function

		Private ReadOnly ds As DataSet
		Private ReadOnly categoriesTable As DataTable
		Private ReadOnly productsTable As DataTable
		Private ReadOnly regionsTable As DataTable
		Private ReadOnly rand As New Random(1)
'INSTANT VB NOTE: The variable prodClasses was renamed since Visual Basic does not allow variables and other class members to have the same name:
		Private ReadOnly prodClasses_Renamed As ProductClasses
'INSTANT VB NOTE: The variable regClasses was renamed since Visual Basic does not allow variables and other class members to have the same name:
		Private ReadOnly regClasses_Renamed As RegionClasses

		Protected ReadOnly Property Regions() As DataRowCollection
			Get
				Return regionsTable.Rows
			End Get
		End Property
		Protected ReadOnly Property Products() As DataRowCollection
			Get
				Return productsTable.Rows
			End Get
		End Property
		Protected ReadOnly Property ProdClasses() As ProductClasses
			Get
				Return prodClasses_Renamed
			End Get
		End Property
		Protected ReadOnly Property RegClasses() As RegionClasses
			Get
				Return regClasses_Renamed
			End Get
		End Property
		Protected ReadOnly Property Random() As Random
			Get
				Return rand
			End Get
		End Property

		Protected Sub New(ByVal ds As DataSet)
			Me.ds = ds
			categoriesTable = ds.Tables("Categories")
			productsTable = ds.Tables("Products")
			regionsTable = ds.Tables("Regions")
			prodClasses_Renamed = New ProductClasses(productsTable.Rows)
			regClasses_Renamed = New RegionClasses(regionsTable.Rows)
		End Sub
		Protected Function GetRegionWeigtht(ByVal region As DataRow) As Double
			Return regClasses_Renamed(DirectCast(region("RegionID"), Integer))
		End Function
		Protected Function GetProductClass(ByVal product As DataRow) As ProductClass
			Return prodClasses_Renamed(DirectCast(product("ProductID"), Integer))
		End Function
		Protected Function GetCategoryName(ByVal product As DataRow) As String
			Return CStr(categoriesTable.Select(String.Format("CategoryID = {0}", product("CategoryID")))(0)("CategoryName"))
		End Function
		Protected Function CreateUnitsSoldGenerator(ByVal regionWeight As Double, ByVal productClass As ProductClass) As UnitsSoldRandomGenerator
			Return New UnitsSoldRandomGenerator(rand, CInt(Fix(Math.Ceiling(productClass.SaleProbability * regionWeight))))
		End Function
		Protected MustOverride Sub Generate(ByVal context As Context)
		Protected Overridable Sub EndGenerate()
		End Sub
		Public Sub Generate()
			For Each region As DataRow In Regions
				Dim state As String = GetState(region)
				Dim regionWeight As Double = GetRegionWeigtht(region)
				For Each product As DataRow In Products
					Dim unitsSoldgenerator As UnitsSoldRandomGenerator = CreateUnitsSoldGenerator(regionWeight, GetProductClass(product))
					Generate(New Context(state, GetProductName(product), GetCategoryName(product), GetListPrice(product), unitsSoldgenerator))
				Next product
			Next region
			EndGenerate()
		End Sub
	End Class

	Public Class UnitsSoldRandomGenerator
		Private Const MinUnitsSold As Integer = 5

		Private ReadOnly rand As Random
		Private ReadOnly startUnitsSold As Integer
		Private prevUnitsSold? As Integer
		Private prevPrevUnitsSold? As Integer
'INSTANT VB NOTE: The variable unitsSold was renamed since Visual Basic does not allow variables and other class members to have the same name:
		Private unitsSold_Renamed As Integer
'INSTANT VB NOTE: The variable unitsSoldTarget was renamed since Visual Basic does not allow variables and other class members to have the same name:
		Private unitsSoldTarget_Renamed As Integer
		Private isFirst As Boolean = True

		Public ReadOnly Property UnitsSold() As Integer
			Get
				Return unitsSold_Renamed
			End Get
		End Property
		Public ReadOnly Property UnitsSoldTarget() As Integer
			Get
				Return unitsSoldTarget_Renamed
			End Get
		End Property

		Public Sub New(ByVal rand As Random, ByVal startUnitsSold As Integer)
			Me.rand = rand
			Me.startUnitsSold = Math.Max(startUnitsSold, MinUnitsSold)
		End Sub
		Public Sub [Next]()
			If isFirst Then
				unitsSold_Renamed = startUnitsSold
				isFirst = False
			Else
				unitsSold_Renamed = unitsSold_Renamed + CInt(Fix(Math.Round(DataHelper.Random(rand, unitsSold_Renamed * 0.5))))
				unitsSold_Renamed = Math.Max(unitsSold_Renamed, MinUnitsSold)
			End If
			Dim unitsSoldSum As Integer = unitsSold_Renamed
			Dim count As Integer = 1
			If prevUnitsSold.HasValue Then
				unitsSoldSum += prevUnitsSold.Value
				count += 1
			End If
			If prevPrevUnitsSold.HasValue Then
				unitsSoldSum += prevPrevUnitsSold.Value
				count += 1
			End If
			unitsSoldTarget_Renamed = CInt(Fix(Math.Round(CDbl(unitsSoldSum) / count)))
			unitsSoldTarget_Renamed = unitsSoldTarget_Renamed + CInt(Fix(Math.Round(DataHelper.Random(rand, unitsSoldTarget_Renamed))))
			prevPrevUnitsSold = prevUnitsSold
			prevUnitsSold = unitsSold_Renamed
		End Sub
	End Class

	Public Class ProductClasses
		Inherits List(Of ProductClass)

		Default Public Shadows ReadOnly Property Item(ByVal productID As Integer) As ProductClass
			Get
				For Each productClass As ProductClass In Me
					If productClass.ContainsProduct(productID) Then
						Return productClass
					End If
				Next productClass
				Throw New ArgumentException("procutID")
			End Get
		End Property

		Public Sub New(ByVal products As ICollection)
			Add(New ProductClass(Nothing, 100D, 0.5))
			Add(New ProductClass(100D, 500D, 0.4))
			Add(New ProductClass(500D, 1500D, 0.3))
			Add(New ProductClass(1500D, Nothing, 0.2))
			For Each product As DataRow In products
				Dim productID As Integer = DirectCast(product("ProductID"), Integer)
				Dim listPrice As Decimal = DirectCast(product("ListPrice"), Decimal)
				For Each productClass As ProductClass In Me
					If productClass.AddProduct(productID, listPrice) Then
						Exit For
					End If
				Next productClass
			Next product
		End Sub
	End Class

	Public Class ProductClass
		Private ReadOnly productIDs As New List(Of Integer)()
		Private ReadOnly minPrice? As Decimal
		Private ReadOnly maxPrice? As Decimal
'INSTANT VB NOTE: The variable saleProbability was renamed since Visual Basic does not allow variables and other class members to have the same name:
		Private ReadOnly saleProbability_Renamed As Double

		Public ReadOnly Property SaleProbability() As Double
			Get
				Return saleProbability_Renamed
			End Get
		End Property

		Public Sub New(ByVal minPrice? As Decimal, ByVal maxPrice? As Decimal, ByVal saleProbability As Double)
			Me.minPrice = minPrice
			Me.maxPrice = maxPrice
			Me.saleProbability_Renamed = saleProbability
		End Sub
		Public Function AddProduct(ByVal productID As Integer, ByVal price As Decimal) As Boolean
			Dim satisfyMinPrice As Boolean = Not minPrice.HasValue OrElse price >= minPrice.Value
			Dim satisfyMaxPrice As Boolean = Not maxPrice.HasValue OrElse price < maxPrice.Value
			If satisfyMinPrice AndAlso satisfyMaxPrice Then
				productIDs.Add(productID)
				Return True
			End If
			Return False
		End Function
		Public Function ContainsProduct(ByVal productID As Integer) As Boolean
			Return productIDs.Contains(productID)
		End Function
	End Class

	Public Class RegionClasses
		Inherits Dictionary(Of Integer, Double)

		Public Sub New(ByVal regions As ICollection)
			Dim numberEmployeesMin? As Integer = Nothing
			For Each region As DataRow In regions
				Dim numberEmployees As Short = DirectCast(region("NumberEmployees"), Short)
				numberEmployeesMin = If(numberEmployeesMin.HasValue, Math.Min(numberEmployeesMin.Value, numberEmployees), numberEmployees)
			Next region
			For Each region As DataRow In regions
				Add(DirectCast(region("RegionID"), Integer), DirectCast(region("NumberEmployees"), Short) / CDbl(numberEmployeesMin.Value))
			Next region
		End Sub
	End Class

	Public NotInheritable Class DataHelper

		Private Sub New()
		End Sub

'INSTANT VB NOTE: The parameter random was renamed since Visual Basic will not allow parameters with the same name as their enclosing function or property:
		Public Shared Function Random(ByVal random_Renamed As Random, ByVal deviation As Double, ByVal positive As Boolean) As Double
			Dim rand As Integer = random_Renamed.Next(If(positive, 0, -1000000), 1000000)
			Return CDbl(rand) / 1000000 * deviation
		End Function
'INSTANT VB NOTE: The parameter random was renamed since Visual Basic will not allow parameters with the same name as their enclosing function or property:
		Public Shared Function Random(ByVal random_Renamed As Random, ByVal deviation As Double) As Double
			Return DataHelper.Random(random_Renamed, deviation, False)
		End Function
	End Class
End Namespace
