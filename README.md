**VSFormsManager**

VSFormsManager is a small Windows Forms utility that uses AI to convert from one Form format 
to another and allow copying forms from project to project. 

<img width="437" height="156" alt="image" src="https://github.com/user-attachments/assets/3f028c76-26e2-41a4-b7b8-eafdbd7fbcaf" />

*Select Forms from a Project*
<img width="983" height="673" alt="image" src="https://github.com/user-attachments/assets/c4316dae-2532-4830-9878-f6e9865dbdee" />

*Save form to another project and convert format (WinForms, WinForms designer, XAML supported)
<img width="783" height="730" alt="image" src="https://github.com/user-attachments/assets/9c907326-adec-4876-a760-bdfbc1d1af45" />

<img width="619" height="589" alt="image" src="https://github.com/user-attachments/assets/6fa80cd9-116b-4d43-9a1f-a39b8b3c433b" />


*Wish List*

Batch copy — select multiple forms from the tree and copy them all to a target project folder at once, with a single dependency review pass

Dependency resolution — if the target is a known .csproj, you could scan its source tree to auto-detect which of the flagged namespaces actually exist there

Conversion history — log what was converted to what, so you can re-run or audit later

VS Extension — the parsing and conversion logic is cleanly separated from the UI, so wrapping it in a VSIX extension and calling it from right-click in Solution Explorer would be straightforward
