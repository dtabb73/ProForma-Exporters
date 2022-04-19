using System;
using System.IO;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace ProFormaFormatter {

    class PSImod {
	public string EntryName="";
	public string OutputName="";
	public int    RoundedMass=0;
	public PSImod Next=null;

	public void PrintAll() {
	    PSImod Runner = this.Next;
	    Console.WriteLine("PrintAll");
	    while (Runner != null) {
		Console.Write(Runner.EntryName);
		Console.Write("\t");
		Console.Write(Runner.OutputName);
		Console.Write("\t");
		Console.WriteLine(Runner.RoundedMass);
		Runner = Runner.Next;
	    }
	}

	public PSImod FindByString(string Key) {
	    PSImod       TailRunner = this.Next;
	    while (TailRunner != null) {
		if (string.Equals(TailRunner.EntryName,Key)) {
			return TailRunner;
		    }
		TailRunner = TailRunner.Next;
	    }
	    return null;
	}
	
	public void LoadFromFile() {
	    PSImod       TailRunner = this;
	    StreamReader SRead = new StreamReader("PSI-MOD.obo.txt");
	    string       WholeLine;
	    string[]     Chunks;
	    float        Mass;
	    while (SRead.Peek() > 0) {
		WholeLine = SRead.ReadLine();
		if (WholeLine.StartsWith("id:")) {
		    TailRunner.Next = new PSImod();
		    TailRunner = TailRunner.Next;
		}
		else {
		    if (WholeLine.StartsWith("name:")) {
			TailRunner.EntryName = WholeLine.Substring(6);
		    }
		    else {
			if (WholeLine.StartsWith("synonym:") && WholeLine.EndsWith("RELATED PSI-MS-label []")) {
			    Chunks = WholeLine.Split('\"');
			    TailRunner.OutputName = Chunks[1];
			}
			else {
			    if (WholeLine.StartsWith("xref: DiffMono:")) {
				Chunks = WholeLine.Split('\"');
				if (Chunks[1] != "none") {
				    Mass = Convert.ToSingle(Chunks[1]);
				    TailRunner.RoundedMass = (int)Math.Round(Mass);
				}
			    }
			}
		    }
		}
	    }
	}

	public void FillEmptyOutputNames() {
	    PSImod TailRunner=this.Next;
	    while (TailRunner != null) {
		if (TailRunner.OutputName.Length == 0) {
		    // I am unsure why, but the PSI-MS name "Acetyl" isn't connected with the term below.
		    if (TailRunner.EntryName == "alpha-amino acetylated residue") {
			TailRunner.OutputName = "Acetyl";
		    }
		    else {
			TailRunner.OutputName = "M: " + TailRunner.EntryName;
		    }
		}
		TailRunner = TailRunner.Next;
	    }
	}
    }
    
    class PTM {
	public int Position=0;
	public string Name="";
	public PTM Next=null;
	
	public void PrintAll() {
	    PTM Runner = this.Next;
	    Console.WriteLine("PrintAll");
	    while (Runner != null) {
		Console.Write(Runner.Position);
		Console.Write("\t");
		Console.WriteLine(Runner.Name);
		Runner = Runner.Next;
	    }
	}
	
	public void InsertSorted(int NewPos, string NewString) {
	    PTM Runner = this;
	    while (Runner.Next != null && Runner.Next.Position < NewPos) {
		Runner = Runner.Next;
	    }
	    PTM PlaceHolder = Runner.Next;
	    Runner.Next = new PTM();
	    Runner = Runner.Next;
	    Runner.Position = NewPos;
	    Runner.Name = NewString;
	    Runner.Next = PlaceHolder;
	}
	
    }
    
    class Program {
	static void Main(string[] args) {
	    if (args.Length==0) {
		Console.WriteLine("Please be sure that this directory contains a copy of PSI-MOD.obo.txt from https://github.com/HUPO-PSI/psi-mod-CV/blob/master/PSI-MOD.obo.");
		Console.WriteLine("Supply the name of the exported *_PrSMs.txt file for processing.");
		Console.WriteLine("If you would like to restrict to medium or high PrSMs, append --medium or --high to the command line.");
		Environment.Exit(1);
	    }
	    float MinNegLogEVal = -700;
	    if (args.Length==2) {
		if (args[1] == "--high") {
		    MinNegLogEVal = 5;
		}
		else if (args[1] == "--medium") {
		    MinNegLogEVal = 2;
		}
	    }
	    DataTable datatable = new DataTable();
	    PSImod PSIModTable = new PSImod();
	    PSImod ThisPSIMod;
	    PSIModTable.LoadFromFile();
	    PSIModTable.FillEmptyOutputNames();
	    string FileString = args[0];
	    StreamReader streamreader = new StreamReader(FileString);
	    string FNameTrailer = ".raw";
	    string WholeLine = streamreader.ReadLine();
	    // Console.WriteLine(WholeLine);
	    string[] columnheaders = WholeLine.Split('\t');
	    foreach (string columnheader in columnheaders) {
		datatable.Columns.Add(columnheader);
	    }
	    while (streamreader.Peek() > 0) {
		DataRow datarow = datatable.NewRow();
		datarow.ItemArray = streamreader.ReadLine().Split('\t');
		datatable.Rows.Add(datarow);
	    }
	    // Remove the coloumn that is the bane of my existence.
	    // datatable.Columns.Remove("\"External Top Down Displays\"");
	    PTM ThesePTMs = new PTM();
	    foreach (DataRow row in datatable.Rows) {
		string FName = Convert.ToString(row["\"Spectrum File\""]);
		// The scan numbers do not appear in the default export from PSPD
		string Scan = Convert.ToString(row["\"Fragmentation Scan(s)\""]);
		string Seq = Convert.ToString(row["\"Annotated Sequence\""]);
		string PTMs = Convert.ToString(row["\"Modifications\""]);
		string OutCharge = Convert.ToString(row["\"Original Precursor Charge\""]);
		string OutAccession = Convert.ToString(row["\"Protein Accessions\""]);
		string OutEVal = Convert.ToString(row["\"-Log E-Value\""]);
		//Since we need the next field as a number, we need to strip away its double-quotes.
		string FScans = Convert.ToString(row["\"# Fragmentation Scans\""]);
		int FragScans = Convert.ToInt32(FScans.Split('\"')[1]);
		// Truncate the double quotes from the Filename field
		string OutFile = FName.Split('\"')[1];
		// Truncate the double quotes from the Accession field
		OutAccession = OutAccession.Split('\"')[1];
		if (OutFile.EndsWith(FNameTrailer)) {
		    OutFile = OutFile.Substring(0,OutFile.Length-FNameTrailer.Length);
		}
		// Truncate the double quotes from the scans field
		Scan = Scan.Split('\"')[1];
		string[] ScanNumbers = Scan.Split(';');
		// Truncate the double quotes from the sequence field, and
		// force all characters in proteoform sequence to uppercase
		string OutSeq = Seq.ToUpperInvariant().Split('\"')[1];
		// Truncate the double quotes from the PTM field
		PTMs = PTMs.Split('\"')[1];
		// Truncate the double quotes from the Charge field
		OutCharge = OutCharge.Split('\"')[1];
		int OutMassAdded = 0;
		// Truncate the double quotes from the EVal field
		OutEVal = OutEVal.Split('\"')[1];
		// Create a decorated sequence by incorporating PTM string data.
		string[] PTMArray = PTMs.Split(';');
		foreach (string ThisPTM in PTMArray) {
		    if (ThisPTM.Length > 0) {
			string[] PTMFields = ThisPTM.Split('(');
			string PosString = Regex.Replace(PTMFields[0], "[^0-9]","");
			string PTMName = PTMFields[1].Split(')')[0];
			string UniModName = "default";
			// For translating ProSight PD PTM names to masses, you will want https://raw.githubusercontent.com/HUPO-PSI/psi-mod-CV/master/PSI-MOD.obo.
			// I am rounding DiffMono values and prefering PSI-MS labels (though the variable is called "UniModName").
			ThisPSIMod = PSIModTable.FindByString(PTMName);
			if (ThisPSIMod == null) {
			    Console.WriteLine("Your PSI-MOD.obo.txt does not contain this PTM: ");
			    Console.WriteLine(PTMName);
			    Environment.Exit(1);
			}
			UniModName =    ThisPSIMod.OutputName;
			OutMassAdded += ThisPSIMod.RoundedMass;
			// When acetylation is positioned on the first side chain, move it to the N-term instead.
			if (PTMName=="alpha-amino acetylated residue" && PosString == "1") {
			    PosString = "0";
			}
			int Pos = Convert.ToInt32(PosString);
			// Add to the sorted linked list of PTMs for this row of file
			ThesePTMs.InsertSorted(Pos,UniModName);
		    }
		}
 		// ThesePTMs.PrintAll();
		// Create a decorated sequence by incorporating PTM string data.
		int CurrentSeqPos = 0;
		StringBuilder DecoratedSeq = new StringBuilder();
		PTM ThisPTMinLL = ThesePTMs.Next;
		while (ThisPTMinLL != null) {
		    if (ThisPTMinLL.Position == 0) {
			DecoratedSeq.Append("[");
			DecoratedSeq.Append(ThisPTMinLL.Name);
			DecoratedSeq.Append("]-");
			DecoratedSeq.Append(OutSeq.Substring(CurrentSeqPos, ThisPTMinLL.Position-CurrentSeqPos));
		    }
		    else {
			DecoratedSeq.Append(OutSeq.Substring(CurrentSeqPos, ThisPTMinLL.Position-CurrentSeqPos));
			DecoratedSeq.Append("[");
			DecoratedSeq.Append(ThisPTMinLL.Name);
			DecoratedSeq.Append("]");
		    }
		    CurrentSeqPos = ThisPTMinLL.Position;
		    ThisPTMinLL = ThisPTMinLL.Next;
		}
		DecoratedSeq.Append(OutSeq.Substring(CurrentSeqPos));
		string OutProForma = Convert.ToString(DecoratedSeq);
		ThesePTMs.Next=null;
		// To make the number of rows of output equal to the number of PrSMs, we will need to write some rows repeatedly.
		if (Convert.ToSingle(OutEVal)>= MinNegLogEVal) {
		    foreach (string OutScan in ScanNumbers) {
			Console.Write(OutFile);
			Console.Write("\t");
			Console.Write(OutScan);
			Console.Write("\t");
			Console.Write(OutCharge);
			Console.Write("\t");
			Console.Write(OutAccession);
			Console.Write("\t");
			Console.Write(OutSeq);
			Console.Write("\t");
			Console.Write(OutMassAdded);
			Console.Write("\t");
			Console.Write(OutProForma);
			Console.Write("\t");
			Console.WriteLine(OutEVal);
		    }
		}
	    }
	}
    }
}
