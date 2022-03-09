using System;
using System.IO;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace ProFormaFormatter {
    
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
			// This approach is obviously terrible, but I didn't want to build in a PSI-Mod reader since I just needed these.
			// For translating ProSight PD PTM names to masses, you will want https://raw.githubusercontent.com/HUPO-PSI/psi-mod-CV/master/PSI-MOD.obo.
			// I am rounding DiffMono values and prefering PSI-MOD labels (though the variable is called "UniModName").
			if (PTMName=="L-gamma-carboxyglutamic acid") {
			    UniModName="d4CbxGlu";
			    OutMassAdded += -44;
			}
			else if (PTMName=="2-pyrrolidone-5-carboxylic acid ") {
			    // Yes, we needed that extra space on the PTM name above; my parser omits the trailer "(Gln)".
			    UniModName="PyrGlu(Glu)";
			    OutMassAdded += -18;
			}
			else if (PTMName=="N6-mureinyl-L-lysine" || PTMName=="L-arginine amide" || PTMName=="L-proline amide" || PTMName=="half cystine") {
			    UniModName="Dehydro";
			    OutMassAdded += -1;
			}
			else if (PTMName=="N6-methyl-L-lysine" || PTMName=="N-methyl-L-alanine" || PTMName=="N-methyl-L-methionine" || PTMName=="L-cysteine methyl ester" || PTMName=="omega-N-methyl-L-arginine") {
			    UniModName="Methyl";
			    OutMassAdded += 14;
			}
			else if (PTMName=="L-methionine sulfoxide") {
			    UniModName="Oxidation";
			    OutMassAdded += 16;
			}
			else if (PTMName=="S-nitrosyl-L-cysteine") {
			    UniModName="Nitrosyl";
			    OutMassAdded += 29;
			}
			else if (PTMName=="alpha-amino acetylated residue") {
			    UniModName="Acetyl";
			    OutMassAdded += 42;
			    //Force the modification to the N-terminus
			    PosString = "0";
			}
			else if (PTMName=="N6-acetyl-L-lysine") {
			    UniModName="Acetyl";
			    OutMassAdded += 42;
			}
			else if (PTMName=="N6,N6,N6-trimethyl-L-lysine" || PTMName=="N,N,N-trimethyl-L-alanine") {
			    UniModName="Trimethyl";
			    OutMassAdded += 42;
			}
			else if (PTMName=="L-beta-methylthioaspartic acid") {
			    UniModName="Methylthio";
			    OutMassAdded += 46;
			}
			else if (PTMName=="iodoacetamide - site C") {
			    UniModName="Carbamidomethyl";
			    OutMassAdded += 57;
			}
			else if (PTMName=="S-phospho-L-cysteine" || PTMName=="O-phospho-L-serine" || PTMName=="O-phospho-L-threonine" || PTMName=="O4'-phospho-L-tyrosine") {
			    UniModName="Phospho";
			    OutMassAdded += 80;
			}
			else if (PTMName=="N6-succinyl-L-lysine") {
			    UniModName="N6-succinyl-L-lysine";
			    OutMassAdded += 100;
			}
			else if (PTMName=="N-myristoylglycine") {
			    UniModName="NMyrGly";
			    OutMassAdded += 210;
			}
			else if (PTMName=="L-isoglutamyl-polyglutamic acid") {
			    // UniProt didn't specify how many Glus!
			    UniModName="GluGlu";
			    OutMassAdded += 258;
			}
			else Console.Error.WriteLine("I don't recognize this PTM: " + PTMName + "\nPos String=" + PosString);
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
