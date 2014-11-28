Set fso		= CreateObject("Scripting.FileSystemObject")

Dim sheetNumber, ContentsId, y_sub, nx
Dim Oder(10,3)

Set objFSO = CreateObject("Scripting.FileSystemObject")
Set f = objFSO.GetFile(WScript.ScriptFullName)
p = f.ParentFolder.Path
srtPath = p & "\" & "config для спецификации по листам.txt"

If Not objFSO.FileExists(srtPath) Then
	msgbox "Файл конфигурации не найден!: " & srtPath
	Set App	= Nothing
	Set Job		= Nothing
	Set Dev	= Nothing
	Set Sheet	= Nothing
	Set Sym	= Nothing
	Set Symtemp      = Nothing
	Set Pin		= Nothing
	Set Cor		= Nothing
	Set Cab 	=Nothing
	Set Dev1_t	= Nothing
	Set Dev2_t	= Nothing
	Set Txt		= Nothing
	Set Cmp	=Nothing
	Set Grp	= Nothing	
	Set fso		= Nothing
	Set zona	= Nothing
	Set ArtId = Nothing
	Set IdLength = Nothing
	Set IdCount = Nothing
	Set IdId = Nothing
	Set f = Nothing
    wscript.quit
End If

Set f = fso.OpenTextFile(srtPath, 1, True)

line = Split(f.ReadLine, " ", -1, 1)

verssc = line(0)

line = Split(f.ReadLine, " ", -1, 1)

len_desc = line(0)

line = Split(f.ReadLine, " ", -1, 1)

SymShapka = line(0)

line = Split(f.ReadLine, " ", -1, 1)

SymLine = line(0)

f_l1 = trim(f.ReadLine)

f_l2 = trim(f.ReadLine)

line = Split(f.ReadLine, " ", -1, 1)

y_min1 = line(0)

line = Split(f.ReadLine, " ", -1, 1)

y_min2 = line(0)

line = Split(f.ReadLine, " ", -1, 1)

len_lim_poz = line(0) 

line = Split(f.ReadLine, " ", -1, 1)

allv = line(0)

line = Split(f.ReadLine, " ", -1, 1)

noum = line(0)

line = Split(f.ReadLine, " ", -1, 1)

nor = line(0)

line = Split(f.ReadLine, " ", -1, 1)

numToatt = line(0)

line = Split(f.ReadLine, " ", -1, 1)

addwire = line(0)

line = Split(f.ReadLine, " ", -1, 1)

len_lim_type = line(0)

line = Split(f.ReadLine, " ", -1, 1)

variant1 = line(0)

line = Split(f.ReadLine, " ", -1, 1)

orig = line(0)

line = Split(f.ReadLine, " ", -1, 1)

len_lim_post = line(0)

line = Split(f.ReadLine, " ", -1, 1)

len_lim_prim = line(0)

line = Split(f.ReadLine, " ", -1, 1)

no_prib = line(0)

line = Split(f.ReadLine, " ", -1, 1)

add_name = line(0)

line = Split(f.ReadLine, " ", -1, 1)

new_l = line(0)

line = Split(f.ReadLine, " ", -1, 1)

use_nomer = line(0)

line = Split(f.ReadLine, " ", -1, 1)

use_kod = line(0)

f.close

Set args = WScript.Arguments
fileName = args.Item(0)
sheetNumber = args.Item(1)
subProjectAttribute = args.Item(2)
subProject = args.Item(3)

Set App 	= ConnectToE3
Set Job		= App.CreateJobObject
Set Dev	= Job.CreateDeviceObject
Set Sheet	= Job.CreateSheetObject
Set Sym	= Job.CreateSymbolObject
Set Symtemp      = Job.CreateSymbolObject
Set Pin		= Job.CreatePinObject
Set Cor		= Job.CreatePinObject
Set Cab 	= Job.CreateDeviceObject
Set Dev1_t	= Job.CreateDeviceObject
Set Dev2_t	= Job.CreateDeviceObject
Set Txt		= Job.CreateTextObject
Set Cmp	= Job.CreateComponentObject
Set Grp	= job.CreateGraphObject
Set zona	= CreateObject("Scripting.Dictionary")
Set ArtId = CreateObject("Scripting.Dictionary")
Set IdLength = CreateObject("Scripting.Dictionary")
Set IdCount = CreateObject("Scripting.Dictionary")

Dim	arrShtIds, all_c_pr_main, allv, numToatt, n_str_sp_sub10, n_str_sp_sub20, n_str_sp_sub30, vse
ReDim	arrDevIds(0)
ReDim	Cab_id(10000)		
ReDim  DevIds_all(1000)
ReDim	arr1(10000)
Dim strpoz(20000)
Dim  out_arr(1000)
Dim  s_p_id(50000)
Dim out_arr_poz2(20000)
Dim typep
Dim nolarr(1000)

Set f = fso.OpenTextFile(fileName)

idCnt = 0

Do While f.AtEndOfStream <> True 
	strTemp = f.SkipLine ' or .ReadLine
	idCnt = idCnt + 1
Loop

f.Close

idCnt = idCnt - 1

ReDim  for_sort(IdCnt,15)

ReDim  Sorted(IdCnt)

ReDim  for_sort2(IdCnt,15)

ReDim  for_sort3(5000,15)

ReDim  for_sort4(5000,15)

ReDim  DevIds_all(idCnt)

ReDim  devSorted(idCnt, 2)

Set f = fso.OpenTextFile(fileName)

i=0

Do Until f.AtEndOfStream
	
	id = f.ReadLine
	
	Cmp.SetId id
	
	CmpClass = Cmp.GetAttributeValue("Class")
	
		if CmpClass<>"Реле" Then
	
		Dev.SetId id
	
		IsTerminal = Dev.IsTerminal
		
		IsCable = Dev.IsCable
		
		CmpFunction = Cmp.GetAttributeValue("Function")
		
		If IsTerminal = 1 And (InStr(CmpFunction, "Заземл") > 0 Or InStr(CmpFunction, "заземл")> 0) Then
			IsGround = 1
		Else
			IsGround = 0
		End If
		
		If IsTerminal = 0 OR IsGround = 1 Then
		
			Name = Dev.GetAssignment & Dev.GetName
				
			If Dev.GetLocation<>"" Then 
				Name = "1|" & Dev.GetLocation & Name ' для сортировки добавляем 1 - изделия с местом всегда впереди изделий без места
			Else
				Name = "2|" & Name
			End If
			
			If IsCable = 1 Then
				Name = "1|" & Cmp.GetName & Name
			Else
				if (InStr(CmpFunction, "Труб") > 0 Or InStr(CmpFunction, "труб") > 0 Or InStr(CmpFunction, "Металлорукав") > 0 Or InStr(CmpFunction, "металлорукав") > 0) Then
					Name = "3|" & Name
				Else
					if (InStr(CmpFunction, "Коробк") > 0 Or InStr(CmpFunction, "коробк")) Then
						Name = "4|" & Name
					Else
						if (InStr(CmpClass, "Приводы") > 0 Or InStr(CmpClass, "приводы") > 0) Then
							Name = "5|" & Name
						Else
							If IsGround = 1 Then
								Name = "6|" & Name
							Else
								Name = "7|" & Name
							End If
						End If
					End If
				End If
			End If
			
			If IsTerminal = 1 Then
		
				pinCnt = Dev.GetPinIds(pinIds)
				
				If (pinCnt > 0) Then
				
					Pin.SetId pinIds(1)
				
					Name = Name&":"&Pin.GetName
					
				End If
				
			End If
			
			devSorted(i,0) = id
		
			devSorted(i,1) = Name
		
			i = i+1
				
		End If
		
	End If
	
Loop

ReDim opts (0,2)
opts(0, 0) = 2
opts(0, 1) = 2	' Tree sorting
opts(0, 2) = 0

App.SortArrayByIndexEx  devSorted, opts

For i = 0 To idCnt

	DevIds_all(i) = devSorted(i,0)

Next

'**************************************

i = 0

Call Processing()

i = i - 1

'***************************************

For  n_so = 0 To i

	id = Sorted(n_so)
	
	cmp.setid id
	
	Dev.setid id
	
	for_sort(n_so,0) = Trim(id)
	
	name = Dev.GetName
	
	IsTerminal = Dev.IsTerminal
	
	if IsTerminal = 1 Then
	
		pinCnt = Dev.GetPinIds(pinIds)
				
		If (pinCnt > 0) Then
				
			Pin.SetId pinIds(1)
				
			Name = Name&":"&Pin.GetName
					
		End If
	
	End If
	
	location = Dev.GetLocation
	
	assignment = Dev.GetAssignment
	
	if location<>"" Then name = location & name
	
	if assignment<>"" Then name = assignment & name
	
	for_sort(n_so,2) = bred(name)
		
	If nor <> 1 and (cmp.GetAttributeValue("razd_sp") <> "" or dev.GetAttributeValue("razd_sp") <> "" ) And dev.GetAttributeValue("poz.obozn.alt") <> "" Then
		
		If dev.GetAttributeValue("razd_sp") <> "" Then
			
			temp_razd = dev.GetAttributeValue("razd_sp")
				
		Else
			
			temp_razd = cmp.GetAttributeValue("razd_sp")
				
		End If
			
		If Trim(get_alt(temp_razd)) = "1" And Dev.GetAttributeValue("poz.obozn.alt") <> "" Then
				
			for_sort(n_so,2) = bred(Dev.GetName)
				
			temp_razd = ""
				
		End If
		
	End If
		
		
	for_sort(n_so,3) = dev.GetComponentName
	
	If dev.GetAttributeValue("not_sp") = "1" Then
	
		for_sort(n_so,2) = ""
		
		for_sort(n_so,3) = ""
		
	End If
	
	If no_prib = 1 And dev.GetAttributeValue("NAME_TEH_POZ") <> "" Then
	
		for_sort(n_so,2) = ""
	
		for_sort(n_so,3) = ""
		
	End If 
	
	If orig = 1 Then 
	
		If dev.IsView Then 
		
			Dev1_t.setid  dev.GetOriginalId
			
			If Dev1_t.GetAttributeValue("not_sp") = "1" Or Dev1_t.GetAttributeValue("NAME_TEH_POZ") <> "" Then 
			
				for_sort(n_so,2) = ""
				
				for_sort(n_so,3) = ""
				
			End If 
			
		End If 
		
	End If 
	
	If for_sort(n_so,2) <> "" And for_sort(n_so,3) <> "" Then vse = vse + 1
	
	for_sort(n_so,4) = dev.GetAttributeValue("coment")
	
	for_sort(n_so,5) = cmp.GetAttributeValue( "Description" )
	
	If Dev.GetAttributeValue("Lenght_m_sp") <> "" Then
	
		for_sort(n_so,6) =  IdLength.Item(id)
		
		for_sort(n_so,13) = "км"
		
		for_sort(n_so,2) = "Null"
		
		If for_sort(n_so,4) = "" Then for_sort(n_so,4) = "км"
		
	ElseIf dev.GetAttributeValue("Lenght_m") <> "" Then 
	
		for_sort(n_so,6) = IdLength.Item(id)
	
		for_sort(n_so,13) = "км"
		
		for_sort(n_so,2) = "Null"
	
		If for_sort(n_so,4) = "" Then for_sort(n_so,4) = "км"
		
	Else
	
		for_sort(n_so,6) = 1
		
		for_sort(n_so,13) = "шт"
		
	End If
	
	If (Dev.GetAttributeValue("NotEnumerate") = "1") Then
	
		If (Dev.GetAttributeValue("IsTube") = "1") Then
		
			for_sort(n_so,6) =  IdLength.Item(id)
			
			for_sort(n_so,13) = "м"
			
			If for_sort(n_so,4) = "" Then for_sort(n_so,4) = "м"
		
		Else
		
			for_sort(n_so,6) =  IdCount.Item(id)
		
			for_sort(n_so,13) = "шт"
		
		End If
		
		for_sort(n_so,2) = "Null"
	
	End if
	
	for_sort(n_so,7) = 0
	
	symkol = dev.GetSymbolIds(PSymids, 1)
	
	If symkol > 0 Then
	
		For Each symidt In PSymids
		
			If symidt <> "" Then
			
				sym.setid symidt
				
				sym.GetSchemaLocation x, y, grid
				
				sp_arr = Split( grid, ".", -1, 1 )
				
				If UBound(sp_arr) > 0 Then  for_sort(n_so,8) = sp_arr(1)
				
				schemaCnt = sym.GetSchematicTypes(schemaTyp)
				
				For j=1 To schemaCnt
				
					If shemaT = schemaTyp(j) Then
					
						for_sort(n_so,14) = symidt
						
						Exit For
						
					End If
					
				Next
				
				grid = ""
				
				If for_sort(n_so,14) = "" Then
				
					for_sort(n_so,14) = symidt
					
				End If
				
			End If
			
		Next
		
	End If
	
Next

For  n_so = 0 To IdCnt 'суммирование количества
	
	For n_so2 = n_so To IdCnt
	
		If n_so2 <> n_so Then
		
			If for_sort(n_so2,0) <> "" And for_sort(n_so2,3) <> "" And  for_sort(n_so,3) = for_sort(n_so2,3) And for_sort(n_so,1) = for_sort(n_so2,1) And for_sort(n_so,10) = for_sort(n_so2,10) And  for_sort(n_so,9) = for_sort(n_so2,9)  Then 
			
				dev.setid for_sort(n_so2,0)
			
				If dev.GetAttributeValue("Lenght_m_sp") <> "" Then
			
					for_sort(n_so,6) = Int(for_sort(n_so,6)) + Int(for_sort(n_so2,6))
					
				ElseIf dev.GetAttributeValue("Lenght_m") <> "" Then
				
					for_sort(n_so,6) = Int(for_sort(n_so,6)) + Int(for_sort(n_so2,6))
					
				Else
				
					for_sort(n_so,6) = for_sort(n_so,6) + 1
					
				End If
				
				for_sort(n_so,2) = for_sort(n_so,2) &"|"& for_sort(n_so2,2)
				
				for_sort(n_so,0) = for_sort(n_so,0) &"|"& for_sort(n_so2,0)
				
				for_sort(n_so,8) = for_sort(n_so,8) &"|"& for_sort(n_so2,8)
				
				for_sort(n_so2,0) = ""
				
				for_sort(n_so2,2) = ""
				
				for_sort(n_so2,3) = ""
				
			End If
			
		End If
		
	Next
	
Next

n_new = -1

For  n_so = 0 To idCnt ' удаление пустышек
	
	If for_sort(n_so,2) <> "" And for_sort(n_so,3) <> "" Then
	
		n_new = n_new + 1
		
		IsGroup = 0
		
		for_sort2(n_new,0) = for_sort(n_so,0)
		
		for_sort2(n_new,1) = for_sort(n_so,1)
		
		If for_sort(n_so,2) <> "" Then 'добавление запятых и многоточий
			
			If for_sort(n_so,0) <> "" Then
			
				out_Ids = Split(for_sort(n_so,0), "|", -1, 1)
				
				If (Ubound(out_ids)>0) Then
				
					If IsNumeric(out_Ids(1)) = True Then
		
						Dev.SetId out_Ids(1)
			
						If Dev.GetAttributeValue("GroupAfterPoint")="1" Then IsGroup = 1
					
					End If
					
				End IF
		
			End If
		
			out_arr_poz = Split(for_sort(n_so,2), "|", -1, 1)
			
			n_poz2 = 0
			
			For Each yyy in out_arr_poz
			
				n_poz2  = n_poz2 + 1
				
				out_arr_poz2(n_poz2) = yyy
								
				If (IsGroup = 1) Then
			
					strlen = Len (yyy)
					
					pointPos = InStrRev(yyy,".")
					
					dashPos = InStrRev(yyy,"-")
				
					str3 = Right(yyy,strlen-pointPos)
					
					str1 = Left (yyy,pointPos-1)
					
					str2 = Right (str1,strlen-dashPos-1)
					
					str1 = Left (str1,dashPos-1)
					
					strpoz(n_poz2) = str1&str3&str2
				
				End If
				
			Next
			
			n_poz = 0
			
			For n_poz = 1 To n_poz2
			
				If (IsGroup=1) Then
					
					t1 = divide(strpoz(n_poz),   ch_p ,   dig_p)
				
					t2 = divide(strpoz(n_poz+1), ch_p2 , dig_p2)
				
					t3 = divide(strpoz(n_poz+2), ch_p3 , dig_p3)
				
				Else
								
					t1 = divide(out_arr_poz2(n_poz),   ch_p ,   dig_p)
				
					t2 = divide(out_arr_poz2(n_poz+1), ch_p2 , dig_p2)
				
					t3 = divide(out_arr_poz2(n_poz+2), ch_p3 , dig_p3)
					
				End If
				
				If Trim(ch_p) = Trim(ch_p2) And Trim(ch_p2) = Trim(ch_p3) And (dig_p) = (dig_p2 - 1) And (dig_p2 - 1) = (dig_p3 - 2) Then 
					
					If open_s = 0 Then 
					
						If fist = 1 Then 
						
							out_str_poz = out_str_poz &","& out_arr_poz2(n_poz) &".."
							
							open_s = 1
							
						Else
						
							out_str_poz = out_str_poz & out_arr_poz2(n_poz) &".."
							
							open_s = 1
							
						End If
						
					End If
					
				Else
				
					If open_s = 1 Then
					
						out_str_poz = out_str_poz & out_arr_poz2(n_poz+1)
						
						n_poz = n_poz + 1
						
						open_s = 0
						
					Else
						
						If fist = 0 Then
						
							out_str_poz = out_str_poz & out_arr_poz2(n_poz)
							
						Else
						
							out_str_poz = out_str_poz &","& out_arr_poz2(n_poz)
							
						End If
						
					End If
					
				End If
				
				ch_p    = ""
				
				ch_p2   = ""
				
				ch_p3   = ""
				
				dig_p   = 0
				
				dig_p2  = 0
				
				dig_p3  = 0
				
				fist    = 1
				
			Next
			
			open_s = 0
			
			fist=0
			
			for_sort(n_so,2) = out_str_poz
			
			out_str_poz = ""
			
			Erase out_arr_poz2
			
			Erase out_arr_poz
			
		End If
		
		for_sort2(n_new,2) = for_sort(n_so,2)
		for_sort2(n_new,3) = for_sort(n_so,3)
		for_sort2(n_new,4) = for_sort(n_so,4)
		for_sort2(n_new,5) = for_sort(n_so,5)
		for_sort2(n_new,6) = for_sort(n_so,6)
		for_sort2(n_new,7) = for_sort(n_so,7)
		for_sort2(n_new,8) = for_sort(n_so,8)
		for_sort2(n_new,9) = for_sort(n_so,9)
		for_sort2(n_new,10) = for_sort(n_so,10)
		for_sort2(n_new,11) = for_sort(n_so,11)
		for_sort2(n_new,12) = for_sort(n_so,12)
		for_sort2(n_new,13) = for_sort(n_so,13)
		for_sort2(n_new,14) = for_sort(n_so,14)
		for_sort2(n_new,15) = for_sort(n_so,15)
		If for_sort2(n_new,13) = "км" Then for_sort2(n_new,6) = for_sort2(n_new,6)/1000		' перевод  в километры
		If for_sort(n_so,2) = "Null" Then for_sort2(n_new,2) = ""
		
	End If
	
Next

For n_so = 0 To n_new 'удаление не уникальных зон листа

	olarr = Split(for_sort2(n_so,8), "|", -1, 1)
	
	For  Each wery In olarr
	
		If wery <> "" Then
		
			If Not  zona.Exists(wery) Then
			
				zona.Add wery, ""
				
			End If
			
		End If
		
	Next
	
	If zona.Count > 0 Then
	
		temparr34 = zona.Keys
		
		For n_so5 = 0 To zona.Count -1
		
			If n_so5 = 0 Then
			
				zlstr = temparr34(n_so5)
				
			Else
			
				zlstr = zlstr & "," & temparr34(n_so5)
				
			End If
			
		Next
		
		Erase temparr34
		
	End If
	
	for_sort2(n_so,8) = zlstr
	
	zlstr = ""
	
	zona.RemoveAll
	
Next 

memou = "Null"
memom = "Null"
memor = "Null"

n_new2=-1

For  n_so = 0 To n_new  ' добавление заголовков с устройством, местом
	
	'If (for_sort2(n_so,1) <> "" And  for_sort2(n_so,1) <> memou) Then
	'	memou = for_sort2(n_so,1)
	'	n_new2 = n_new2 + 1 
	'	for_sort3(n_new2,5) = for_sort2(n_so , 1)
	'	for_sort3(n_new2,7) = 2 ' это заголовок (устройство)
	'End If
	
	'If  (for_sort2(n_so,10) <> "" And for_sort2(n_so,10) <> memom ) Then
	'	memom = for_sort2(n_so,10)
	'	n_new2 = n_new2 + 1 
	'	for_sort3(n_new2,5) = for_sort2(n_so , 10)
	'	for_sort3(n_new2,7) = 1 ' это заголовок (место)
	'End If
	
	'If  (for_sort2(n_so,9) <> "" And for_sort2(n_so,9) <> memor ) Then
	'	memor = for_sort2(n_so,9)
	'	n_new2 = n_new2 + 1
	'	for_sort3(n_new2,5) = for_sort2(n_so,9)
	'	for_sort3(n_new2,7) = 3 ' это заголовок (Раздел)
	'End If 
	
	n_new2 = n_new2 + 1 
	for_sort3(n_new2,0) = for_sort2(n_so,0)
	for_sort3(n_new2,1) = for_sort2(n_so,1)
	for_sort3(n_new2,2) = for_sort2(n_so,2)
	for_sort3(n_new2,3) = for_sort2(n_so,3)
	for_sort3(n_new2,4) = for_sort2(n_so,4)
	for_sort3(n_new2,5) = for_sort2(n_so,5)
	for_sort3(n_new2,6) = for_sort2(n_so,6)
	for_sort3(n_new2,7) = for_sort2(n_so,7)
	for_sort3(n_new2,8) = for_sort2(n_so,8)
	for_sort3(n_new2,9) = for_sort2(n_so,9)
	for_sort3(n_new2,10) = for_sort2(n_so,10)
	for_sort3(n_new2,11) = for_sort2(n_so,11)
	for_sort3(n_new2,12) = for_sort2(n_so,12)
	for_sort3(n_new2,13) = for_sort2(n_so,13)
	for_sort3(n_new2,14) = for_sort2(n_so,14)
	for_sort3(n_new2,15) = for_sort2(n_so,15)
	
Next

n_new3=-1

For  n_so = 0 To n_new2 ' добавление перенесенных строк
		
	max_perenos = 0
	
	'for_sort3(n_so,5) = replace_razd2(for_sort3(n_so,5))
	
	call	split_str(for_sort3(n_so,2),  out_arr10, len_lim_poz ,n_str_sp_sub10,",", "..")' с поз. обозн.
	call	split_str(for_sort3(n_so,5) ,  out_arr20, len_desc     ,n_str_sp_sub20," ", "")' с наименованием
	for_sort3(n_so,3) = ""
	'call	split_str(for_sort3(n_so,3),  out_arr30, len_lim_type ,n_str_sp_sub30," ", "-")' тип
	'call	split_str(for_sort3(n_so,12), out_arr40, len_lim_post ,n_str_sp_sub40," ", "")' поставщик
	call	split_str(for_sort3(n_so,4),  out_arr50, len_lim_prim ,n_str_sp_sub50," ", "")' примечание 
	
	If use_nomer = "1" Then
	
		spaceArr = Split(for_sort3(n_so,15), " ")	' подсчет количества вхождений пробела и тире
		
		spaceCnt = Ubound(spaceArr)
		
		dashArr = Split(for_sort3(n_so,15), "-")
		
		dashCnt = Ubound(dashArr)
		
		If (spaceCnt>=dashCnt) Then
			
			call	split_str(for_sort3(n_so,15),  out_arr60,  len_lim_post ,n_str_sp_sub60," ", "")' номер по каталогу (разделение по пробелу)
			
		Else
			
			call	split_str(for_sort3(n_so,15),  out_arr60, len_lim_post ,n_str_sp_sub60,"-", "")' номер по каталогу (разделение по тире)
			
		End If
		
	End If   	
	
	ReDim ABC(10)
	ABC(1) = n_str_sp_sub10
	ABC(2) = n_str_sp_sub20
	ABC(3) = n_str_sp_sub30
	ABC(4) = n_str_sp_sub40
	ABC(5) = n_str_sp_sub50
	
	If use_nomer = "1" Then ABC(6) = n_str_sp_sub60
		
	For maxn = 1 To 6
		
		If max_perenos < ABC(maxn) Then max_perenos = ABC(maxn)
		
	Next
	
	If max_perenos > 1 Then
		
		n_new3 = n_new3 + 1 
		for_sort4(n_new3,0) = for_sort3(n_so,0)
		for_sort4(n_new3,1) = for_sort3(n_so,1)
		for_sort4(n_new3,2) = out_arr10(1)
		'for_sort4(n_new3,3) = out_arr30(1)
		for_sort4(n_new3,3) = for_sort3(n_so,3)
		for_sort4(n_new3,4) = out_arr50(1)
		for_sort4(n_new3,5) = out_arr20(1)
		for_sort4(n_new3,6) = for_sort3(n_so,6)
		for_sort4(n_new3,7) = for_sort3(n_so,7)
		for_sort4(n_new3,8) = for_sort3(n_so,8)
		for_sort4(n_new3,9) = for_sort3(n_so,9)
		for_sort4(n_new3,10) = for_sort3(n_so,10)
		for_sort4(n_new3,11) = for_sort3(n_so,11)
		'for_sort4(n_new3,12) = out_arr40(1)
		for_sort4(n_new3,12) = for_sort3(n_so,12)
		for_sort4(n_new3,13) = for_sort3(n_so,13)
		for_sort4(n_new3,14) = for_sort3(n_so,14)
		for_sort4(n_new3,15) = out_arr60(1)
		
		For add_st = 2 To max_perenos
			
			n_new3 = n_new3 + 1
			
			If n_str_sp_sub10 <= max_perenos   Then
				
				for_sort4(n_new3,2) = out_arr10(add_st)
				
			End If
			
			If n_str_sp_sub20 <= max_perenos   Then
				
				for_sort4(n_new3,5) = out_arr20(add_st)
				
			End If
			
			'If n_str_sp_sub30 <= max_perenos   Then
				
			'	for_sort4(n_new3,3) = out_arr30(add_st)
				
			'End If
				
			'If n_str_sp_sub40 <= max_perenos   Then
				
			'	for_sort4(n_new3,12) = out_arr40(add_st)
					
			'End If
				
			If n_str_sp_sub50 <= max_perenos   Then
				
				for_sort4(n_new3,4) = out_arr50(add_st)
				
			End If
			
			If use_nomer = "1" Then
				
				If n_str_sp_sub60 <= max_perenos   Then
					
					for_sort4(n_new3,15) = out_arr60(add_st)
					
				End If
					
			End If
			
		Next
		
	Else
		
		n_new3 = n_new3 + 1 
		for_sort4(n_new3,0) = for_sort3(n_so,0)
		for_sort4(n_new3,1) = for_sort3(n_so,1)
		for_sort4(n_new3,2) = for_sort3(n_so,2)
		for_sort4(n_new3,3) = for_sort3(n_so,3)
		for_sort4(n_new3,4) = for_sort3(n_so,4)
		for_sort4(n_new3,5) = for_sort3(n_so,5)
		for_sort4(n_new3,6) = for_sort3(n_so,6)
		for_sort4(n_new3,7) = for_sort3(n_so,7)
		for_sort4(n_new3,8) = for_sort3(n_so,8)
		for_sort4(n_new3,9) = for_sort3(n_so,9)
		for_sort4(n_new3,10) = for_sort3(n_so,10)
		for_sort4(n_new3,11) = for_sort3(n_so,11)
		for_sort4(n_new3,12) = for_sort3(n_so,12)
		for_sort4(n_new3,13) = for_sort3(n_so,13)
		for_sort4(n_new3,14) = for_sort3(n_so,14)
		for_sort4(n_new3,15) = for_sort3(n_so,15)
		
	End If
	
Next

Call vivod(for_sort4, n_new3,"A4",newsheetf,newsheetrf)

Set App	= Nothing
Set Job		= Nothing
Set Dev	= Nothing
Set Sheet	= Nothing
Set Tree	= Nothing
Set Sym	= Nothing
Set Symtemp      = Nothing
Set Pin		= Nothing
Set Cor		= Nothing
Set Cab 	=Nothing
Set Dev1_t	= Nothing
Set Dev2_t	= Nothing
Set Txt		= Nothing
Set Cmp	=Nothing
Set Grp	= Nothing
Set fso		= Nothing
Set zona	= Nothing
Set ArtId = Nothing
Set IdLength = Nothing
Set IdCount = Nothing
Set IdId = Nothing
Set f = Nothing
WScript.Quit

Sub AddArray (arr1, arr2)
	Dim i
	Dim ub1, lb1, cnt1
	Dim ub2, lb2, cnt2    
	ub1  = UBound(arr1)
	lb1  = LBound(arr1)
	cnt1  = ub1 - lb1
	ub2  = UBound(arr2)
	lb2  = LBound(arr2)
	cnt2 = ub2 - lb2
	
	ReDim Preserve arr1 (cnt1 + cnt2)
	
	For i = lb2+1 To ub2
	
		arr1(ub1) = arr2(i)
		ub1 = ub1 + 1
		
	Next

End Sub

Sub vivod(for_sort_sub, comp_n, typep, newsh,newshr)
	
	If typep = "A4" Then
	
		Sheet.Create 0,sheetNumber , f_l1, ContentsId , 0
		ContentsId = Sheet.getid
		perviy = ContentsId
		f_l1 = f_l2 
		
	Else
	
		ContentsId = Job.GetCursorPosition( xpos, ypos ) 
		sheet.setid ContentsId
		sheet.GetSymbolIds allsy
		
		For Each pl In allsy ' удаление старых символов на листе
		
			Sym.Setid pl
			
			If Sym.GetSymbolTypeName = SymShapka Or Sym.GetSymbolTypeName = SymLine Then
			
				Sym.Delete
				
			End If
			
		Next
		
	End If
	
	Sheet.SetAttributeValue subProjectAttribute, subProject
	
	SymId = Sym.Load (SymShapka, "")
	
	If SymId <> 0 Then
	
	Else
	
		MsgBox "Символ "& SymShapka  &  " не найден в БД!"
		WScript.quit
		
	End if 
	
	SymId = Sym.Place (ContentsId, 0, -10, 0)
	
	all_s_c = all_s_c + 1
	s_p_id(all_s_c) = Sym.Getid
	
	SymId = Sym.Load (SymLine, "")
	
	If SymId <> 0 Then
	
	Else
	
		MsgBox "Символ "& SymLine  &  " не найден в БД!"
		WScript.quit
		
	End if
	
	Sym.setid SymId
	
	y_sub = -17 
	
	For n_sub = 0 To comp_n
		
		SymId = Sym.Load (SymLine, "")
		
		y_sub = y_sub - 8
		
		SymId = Sym.Place (ContentsId, 0, y_sub , 0) 
		
		all_s_c = all_s_c + 1
		
		s_p_id(all_s_c) = Sym.Getid 
		
		Sym.setid SymId
		
		Sym.SetAttributeValue "column2", for_sort_sub(n_sub,2)
		Sym.SetAttributeValue "column6", for_sort_sub(n_sub,4)
		Sym.SetAttributeValue "column5", for_sort_sub(n_sub,5)
		Sym.SetAttributeValue "column4", for_sort_sub(n_sub,6)
		
		If typep <> "A4" then
		
			If (y_sub < CInt(y_c_m.value))  Then' сдвиг таблицы 
			
				For ui = 1 To all_s_c
				
					symtemp.setid s_p_id(ui)
					
					symtemp.GetSchemaLocation x, y, grid
					
					symtemp.Place ContentsId, x - shirina, y, 0
					
				Next 
				
				y_sub = ymax_l + (ySymMax - ySymMin)
			
			End If
			
		Else
		
			If y_sub < CInt(y_min1) Then
			
				Call crnewsh()
			
			End if
		
		End If
		
	Next
	
End Sub

Sub crnewsh()
	
	sheetNumber = sheetNumber + 1
	Sheet.Create 0,sheetNumber , f_l1, ContentsId , 0
	ContentsId = Sheet.getid
	
	Sheet.SetAttributeValue subProjectAttribute, subProject
	
	y_min1 = y_min2
	
	SymId = Sym.Load (SymShapka, "")
	
	If SymId <> 0 Then
	
	Else
	
		MsgBox "Символ "& SymShapka  &  " не найден в БД!"
		WScript.quit
		
	End if
	
	SymId = Sym.Place(ContentsId, 0, -10, 0)
	
	SymId = Sym.Load (SymLine, "")
	
	If SymId <> 0 Then
	
	Else
	
		MsgBox "Символ "& SymLine  &  " не найден в БД!"
		WScript.quit
		
	End if
	
	Sym.setid SymId
	
	y_sub = - 17
	
End Sub 

Function divide(Text,Text1,Text22)

	length_Dev = Len(Text)
	nSymbol = 0
	Text1 = ""
	Text2 = ""
	Flag_num = 0
	
	If Text = "" Then Flag_num = 1
	
	While Flag_num = 0
	
		letter = Mid(Text,length_Dev-nSymbol,1)
		nSymbol = nSymbol + 1
		char_code = Asc( letter )
		
		If char_code > 47 and char_code < 58 Then
		
			Text2 = letter & Text2
			
		Else
		
			Text1 = Trim(Left(Text,length_Dev-nsymbol+1))
			
			Flag_num = 1
			
		End If
		
		If nSymbol = length_Dev Then Flag_num = 1
		If Flag_num = 1 and Text2 <> "" Then Text22 = cint(Trim(Text2))

	Wend
	
End function

Function get_alt(in_razd)

	For nx_t = 1 To nx
	
		If in_razd = Oder(nx_t, 0) Then 
		
			get_alt = Oder(nx_t, 3) 
			
			Exit for
			
		End If 
		
	Next
	
End Function

Function replace_razd1(in_razd) 
	
	For nx_t = 1 To nx
		
		If in_razd = Oder(nx_t, 0) Then
		
			replace_razd1 = Oder(nx_t, 2)
			
			Exit for
			
		End If
		
		replace_razd1 = in_razd
		
	Next

End function  

Function replace_razd2(in_razd2)

	For nx_t2 = 1 To nx
	
		If in_razd2 = Oder(nx_t2, 2) Then
		
			replace_razd2 = Oder(nx_t2, 0)
			
			Exit for
			
		End If
		
		replace_razd2 = in_razd2	
 
	Next
	
End Function

Sub split_str(in_str, out_arr, len_lim,n_str_sp_sub,spliter, spliter2)
	
	ReDim   out_arr(50000)
	ReDim   arr_str2(50000)
	
	arr_str = Split(in_str, spliter, -1, 1)
	
	If spliter2 <> "" And spliter2<> "~" Then
	
		For Each str2 In arr_str
		
			stn3 = stn3 + 1
			
			If stn2 > 1 And str2 <> in_str  Then str2 = spliter & str2
			
			arr_str2_t = Split(str2, spliter2, -1, 1)
				
			For Each str3 In arr_str2_t
				
				If stn2 < 1 And str3 <> str2  Then
				
					arr_str2(stn2) = str3
					
				Else
				
					arr_str2(stn2) = spliter2 & str3
					
				End If
				
				stn2 = stn2 + 1
				
			Next
			
		Next
		
		Erase  arr_str
		
		arr_str = arr_str2
		
	Else
	
		For Each str2 In arr_str
		
			If stn2 < 1 Then
			
				arr_str(stn2) = str2
				
			Else
			
				arr_str(stn2) = spliter & str2
				
			End If
			
			stn2 = stn2 + 1
			
		Next
		
	End If
	
	If spliter2="~" Then
		
		strCnt = Ubound(arr_str)
		
		If strCnt>0 Then
		
			i = 0
		
			For Each str in arr_str
			
				If str = "" Then Exit For
		
				If ( i= 0 ) Then 
				
					arr_str(i) = str+spliter
				
				Else
				
					str = Replace (str,",","")
					
					arr_str(i) = str+spliter
				
					If i = strCnt Then 
					
						arr_str(i) = str
						
					Else
					
						arr_str(i) = str+spliter
						
					End If
					
				End If
			
				i = i+1
		
			Next
			
		End If
		
	End If
	
	For Each str In arr_str
	
		If str = "" Then Exit For
	
		all_part = all_part + 1
		
	Next
	
	For Each str In arr_str
	
		all_part2 = all_part2 + 1
		
		If str = "" Then Exit For
		
		If str_t = "" Then
		
			str_t = str
			
		Else
		
			str_t = str_t &  str
			
		End If
		
		If all_part2 <> all_part Then dovesok = Len(arr_str(all_part2))
		
		If (Len(str_t) + dovesok)  > CInt (Trim(len_lim)) Then
		
			nstr_arr = nstr_arr +1
			
			If all_part2 <> all_part Then str_t =  str_t
			
			out_arr(CInt(nstr_arr)) = Trim(str_t)
			
			str_t = ""
			
		End if
		
	Next
	
	If str_t <> "" And nstr_arr > 0  Then
	
		nstr_arr = nstr_arr +1
		
		out_arr(CInt(nstr_arr)) = Trim(str_t)
		
	End If
	
	If nstr_arr = 0 Then out_arr(1) = in_str
	
	n_str_sp_sub = nstr_arr
    
	nstr_arr = 0
    
	str_t=""
    
	str=""
	
End Sub

Sub Processing()
	
	For  n_so = 0 To IdCnt
		
		Id = DevIds_all(n_so)
		
		Dev.SetId Id
		
		If (Dev.GetAttributeValue("NotEnumerate")<>"1" And Dev.IsCable=0) Then
						
			Sorted(i) =  Id
				
			i = i + 1
				
		Else
			
			Cmp.SetId Id
				
			ArticleNumber = Cmp.GetAttributeValue("ArticleNumber")
						
				
			If (Dev.GetAttributeValue("IsTube")="1" Or Dev.IsCable = 1) Then
							
				If (Dev.IsCable = 0) Then
						
					strlen = Replace (Dev.GetAttributeValue("dlina_metallorukava"),".",",")
						
				Else
					
					strlen = Replace (Dev.GetAttributeValue("Lenght_m_sp"),".",",")
					
				End If
						
				If  (strlen ="") Then 
					
					App.PutError 0, "У следующего устройства не задана длина: " & Dev.GetName
							
				Else
					
					Length = cSng(strlen)
						
					If Not ArtId.Exists(ArticleNumber) Then
					
						ArtId.Add ArticleNumber, Id
							
						Sorted(i) = Id
		
						i = i + 1
							
						If IdLength.Exists(Id) Then IdLength.Remove(Id)
						
						IdLength.Add Id, Length
						
					Else
					
						IdLength.Item(ArtId.Item(ArticleNumber)) = IdLength.Item(ArtId.Item(ArticleNumber)) + Length
						
					End If
						
				End If
					
			Else
							
				If Not ArtId.Exists(ArticleNumber) Then
					
					ArtId.Add ArticleNumber, Id
							
					Sorted(i) = Id
		
					i = i + 1
							
					If IdCount.Exists(Id) Then IdCount.Remove(Id)
						
					IdCount.Add Id, 1
						
				Else
					
					IdCount.Item(ArtId.Item(ArticleNumber)) = IdCount.Item(ArtId.Item(ArticleNumber)) + 1
						
				End If
						
			End If
				
		End If
		
	Next

End Sub

Function bred(str)
	
	If  Left(str,1) = "-" Then
	
		bred = Right(str, Len(str)-1)
	
	Else
	
		bred = str
		
	End If 

End Function

Function ConnectToE3
	If InStr(WScript.FullName, "Eі") Then
		Set ConnectToE3 = WScript										' internal -> connect directly
	Else
		On Error Resume Next											' to skip error, if no dispatcher is installed
		Set disp   = CreateObject("CT.Dispatcher")        				' external
		Set viewer = CreateObject("CT.DispatcherViewer")
		On Error GoTo 0
		Set ConnectToE3 = Nothing
	    If IsObject(disp) Then											' test if E3.Dispatcher is installed  
			ProcessCnt = disp.GetE3Applications(lst)					' read active E3 processes
			If ProcessCnt > 1 Then										' more than 1 process, ask for the project to connect 
				' >>>>>>>>>>>>>>>>>>>>
	            ' Start DispatcherViewer and use the dispatcher interface for selecting an E3 process
	            If viewer.ShowViewer(e3Obj) = True Then												' display dispatcher interface to select process
				   Set ConnectToE3 = e3Obj
				Else
					MsgBox "Что - то не так"	' !!!!! as long as bug in dispatcher viewer is available
					wscript.quit
				End If
			Else
				Set ConnectToE3 = CreateObject("CT.Application")
	        End If
		Else
			strComputer = "."																	' dispatcher not installed
			Set objWMIService = GetObject("winmgmts:\\" & strComputer & "\root\cimv2")
			Set colItems      = objWMIService.ExecQuery("Select * from Win32_Process",,48)
			ProcessCnt = 0
			For Each objItem In colItems
				If InStr(objItem.Caption, "E3.series") Then ProcessCnt = ProcessCnt + 1
			Next
			Set objWMIService = Nothing
			Set colItems      = Nothing
			If ProcessCnt > 1 Then
				MsgBox  "More than one E3-Application running. Script can't run as external program." & vbCrLf & "Please close the other E3-Applications.", 48
				WScript.Quit
			Else
				Set ConnectToE3 = CreateObject ("CT.Application")		' external
			End if
		End If
		Set disp   = nothing
		Set viewer = nothing
	End if
End Function