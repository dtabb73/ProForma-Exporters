using System;
using System.IO;
using System.Data;
using System.Text;

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

namespace ProFormaFormatter {
    class Program {
	static void Main(string[] args)
	{
	    if (args.Length==0) {
		Console.WriteLine("Supply the name of the search_task _filter.csv or pTop_filtered.csv file for processing.");
		Environment.Exit(1);
	    }
	    DataTable datatable = new DataTable();
	    string FileString = args[0];
	    StreamReader streamreader = new StreamReader(FileString);
	    char[] delimiter = new char[] { ',' };
	    string[] columnheaders = streamreader.ReadLine().Split(delimiter);
	    foreach (string columnheader in columnheaders)
	    {
		datatable.Columns.Add(columnheader);
	    }
	    
	    while (streamreader.Peek() > 0)
	    {
		DataRow datarow = datatable.NewRow();
		datarow.ItemArray = streamreader.ReadLine().Split(delimiter);
		datatable.Rows.Add(datarow);
	    }
	    PTM ThesePTMs = new PTM();
	    // Loop through all the different PrSMs in this table
	    foreach (DataRow row in datatable.Rows) {
		string FName = Convert.ToString(row["Title"]);
		string Seq;
		// This column changes names between pTop 1 and pTop 2
		if (datatable.Columns.Contains("Sequence"))
		    Seq = Convert.ToString(row["Sequence"]);
		else
		    Seq = Convert.ToString(row["Protein Sequence"]);
		string OutSeq = Seq;
		int    OutMassAdded = 0;
		string PTMs = Convert.ToString(row["PTMs"]);
		//pTop output table lacks Q-values
		double EVal;
		if (datatable.Columns.Contains(" Evalue"))
		    EVal = -Math.Log(Convert.ToDouble(row[" Evalue"]));
		else
		    EVal = -Math.Log(Convert.ToDouble(row["Final Score"]));
		string[] FNameChunks = FName.Split('.');
		//This code will break the first time someone uses a period in their RAW file names...
		string OutFile = FNameChunks[0];
		string OutScan = FNameChunks[1];
		string OutCharge = Convert.ToString(row["Charge State"]);
		string OutAccession = Convert.ToString(row["Protein AC"]);
		string[] PTMArray = PTMs.Split(';');
		// Clean away the extra fields for accessions
		OutAccession = OutAccession.Split('|')[1];
		foreach (string ThisPTM in PTMArray) {
		    if (ThisPTM.Length > 0) {
			string[] PTMFields = ThisPTM.Split(')');
			if (PTMFields[0]!="NULL") {
			    int Pos = Convert.ToInt32(PTMFields[0].Substring(1));
			    string[] NameSplitter = PTMFields[1].Split('[');
			    string ModName = NameSplitter[0];
			    if (ModName == "Oxidation")
				OutMassAdded += 16;
			    else if (ModName == "Methyl")
				OutMassAdded += 14;
			    else if (ModName == "Acetyl")
				OutMassAdded += 42;
			    else if (ModName == "Carbamidomethyl")
				OutMassAdded += 57;
			    else if (ModName == "Phospho")
				OutMassAdded += 80;
			    // Add to the sorted linked list of PTMs
			    ThesePTMs.InsertSorted(Pos, ModName);
			}
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
			DecoratedSeq.Append(Seq.Substring(CurrentSeqPos, ThisPTMinLL.Position-CurrentSeqPos));
		    }
		    else {
			DecoratedSeq.Append(Seq.Substring(CurrentSeqPos, ThisPTMinLL.Position-CurrentSeqPos));
			DecoratedSeq.Append("[");
			DecoratedSeq.Append(ThisPTMinLL.Name);
			DecoratedSeq.Append("]");
		    }
		    CurrentSeqPos = ThisPTMinLL.Position;
		    ThisPTMinLL = ThisPTMinLL.Next;
		}
		DecoratedSeq.Append(Seq.Substring(CurrentSeqPos));
		string OutProForma = Convert.ToString(DecoratedSeq);
		// I feel guilty for dumping garbage collection on the interpreter.
		ThesePTMs.Next=null;
		
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
		Console.WriteLine(EVal);
	    }
	}
    }
}
