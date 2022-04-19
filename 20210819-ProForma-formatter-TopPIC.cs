using System;
using System.IO;
using System.Data;
using System.Text;

namespace ProFormaFormatter {

    class PTM {
	public int Position=0;
	public string Name="";
	public PTM Next=null;
	
	public void PrintAll() {
	    PTM Runner = this.Next;
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
	static void Main(string[] args)
	{
	    bool SuppressUnknowns = false;
	    if (args.Length==0) {
		Console.WriteLine("Supply the name of the _ms2_toppic_prsm.tsv file for processing.");
		Console.WriteLine("If you want to exclude unknown mass PTM-containing PrSMs, add the argument \"--suppress\".");
		Environment.Exit(1);
	    }
	    if (args.Length==2) {
		if (args[1] == "--suppress") {
		    SuppressUnknowns = true;
		}
	    }
	    DataTable datatable = new DataTable();
	    StreamReader streamreader = new StreamReader(args[0]);
	    //skip the header
	    string Linebuffer=streamreader.ReadLine();
	    while ((Linebuffer.Length == 0) || (Linebuffer.Substring(0,9) != "Data file")) {
		Linebuffer=streamreader.ReadLine();
	    }
	    char[] delimiter = new char[] { '\t' };
	    char[] PathDelimiters = new char[] { '\\','/' };
	    string FNameTrailer = "_ms2.msalign";
	    string[] columnheaders = Linebuffer.Split(delimiter);
	    foreach (string columnheader in columnheaders)
	    {
		datatable.Columns.Add(columnheader); // I've added the column headers here.
	    }
	    
	    while (streamreader.Peek() > 0)
	    {
		DataRow datarow = datatable.NewRow();
		Linebuffer = streamreader.ReadLine();
		datarow.ItemArray = Linebuffer.Split(delimiter);
		datatable.Rows.Add(datarow);
	    }

	    string LastPrsmID = "NoneSoFar";
	    foreach (DataRow row in datatable.Rows) {
		bool   ContainsUnknown = false;
		string ThisPrsmID = Convert.ToString(row["Prsm ID"]);
		if (ThisPrsmID != LastPrsmID) {
		    LastPrsmID = ThisPrsmID;
		    string OutFile = Convert.ToString(row["Data file name"]);
		    string OutScan = Convert.ToString(row["Scan(s)"]);
		    string OutCharge = Convert.ToString(row["Charge"]);
		    string OutAccession = Convert.ToString(row["Protein accession"]);
		    string Seq = Convert.ToString(row["Proteoform"]);
		    string OutSequence;
		    int    OutMassAdded = 0;
		    double OutEVal = -Math.Log(Convert.ToDouble(row["E-value"]));
		    string[] FNameChunks = OutFile.Split(PathDelimiters);
		    OutFile = FNameChunks[FNameChunks.Length-1];
		    if (OutFile.EndsWith(FNameTrailer)) {
			OutFile = OutFile.Substring(0,OutFile.Length-FNameTrailer.Length);
		    }
		    //FChunk is now the input mzML / RAW without suffix
		    OutAccession = OutAccession.Split('|')[1];
		    //Scan numbers are fine as is
		    //Remove the sequence context characters (and double-quotes) from N-terminus
		    if (Seq[1] == '.')
			Seq = Seq.Substring(2);
		    else
			Seq = Seq.Substring(3);
		    //Remove the sequence context characters from C-terminus
		    if (Seq[Seq.Length-2] == '.')
			Seq = Seq.Substring(0,Seq.Length-2);
		    else
			Seq = Seq.Substring(0,Seq.Length-3);
		    StringBuilder OutSeqBuilder = new StringBuilder();
		    StringBuilder UnkPTMLabel;
		    float ObsMass;
		    PTM ThesePTMs = new PTM();
		    // THIS CODE ASSOCIATES EACH PTM WITH THE LAST SEQUENCE LETTER IN THE RANGE TO WHICH IT MIGHT BE ATTACHED; LOCALIZATION INFORMATION IS DESTROYED HERE.
		    // Note that TopMG results frequently include multiple PTMs in the same ambiguous sequence range; all PTMs for that range are attributed to the last residue in it.
		    foreach (string sElement in Seq.Split('(',')','[',']',';')) {
			if (sElement == "Xlink:Disulfide") {
			    OutMassAdded -= 2;
			    ThesePTMs.InsertSorted(OutSeqBuilder.Length,sElement);
			}
			else if (sElement == "Methyl") {
			    OutMassAdded += 14;
			    ThesePTMs.InsertSorted(OutSeqBuilder.Length,sElement);
			}
			else if (sElement == "Oxidation") {
			    OutMassAdded += 16;
			    ThesePTMs.InsertSorted(OutSeqBuilder.Length,sElement);
			}
			else if (sElement == "Acetyl") {
			    OutMassAdded += 42;
			    // We force N-terminal acetyls to the N-terminus rather than the first side chain
			    ThesePTMs.InsertSorted(0,sElement);
			}
			else if (sElement == "Phospho") {
			    OutMassAdded += 80;
			    ThesePTMs.InsertSorted(OutSeqBuilder.Length,sElement);
			}
			else if (sElement == "Carbamidomethyl") {
			    OutMassAdded += 57;
			    ThesePTMs.InsertSorted(OutSeqBuilder.Length,sElement);
			}
			else if (sElement.Length > 0) {
			    try {
				ObsMass = Convert.ToSingle(sElement);
				// sElement IS a number if you reach this
				ContainsUnknown = true;
				OutMassAdded += (int)Math.Round(ObsMass);
				UnkPTMLabel = new StringBuilder();
				if (sElement[0]!='-') {
				    UnkPTMLabel.Append("Obs:+");
				}
				else {
				    UnkPTMLabel.Append("Obs:");
				}
				UnkPTMLabel.Append(ObsMass);
				ThesePTMs.InsertSorted(OutSeqBuilder.Length,Convert.ToString(UnkPTMLabel));
			    }
			    catch (FormatException) {
				// sElement is NOT a number; it is therefore sequence we can simply append to what is already captured.
				OutSeqBuilder.Append(sElement);
			    }
			}
		    }
		    // ThesePTMs.PrintAll();
		    OutSequence = Convert.ToString(OutSeqBuilder);
		    // Create a decorated sequence by incorporating PTM string data.
		    int CurrentSeqPos = 0;
		    StringBuilder DecoratedSeq = new StringBuilder();
		    PTM ThisPTMinLL = ThesePTMs.Next;
		    while (ThisPTMinLL != null) {
			if (ThisPTMinLL.Position == 0) {
			    DecoratedSeq.Append("[");
			    DecoratedSeq.Append(ThisPTMinLL.Name);
			    DecoratedSeq.Append("]-");
			    DecoratedSeq.Append(OutSequence.Substring(CurrentSeqPos, ThisPTMinLL.Position-CurrentSeqPos));
			}
			else {
			    DecoratedSeq.Append(OutSequence.Substring(CurrentSeqPos, ThisPTMinLL.Position-CurrentSeqPos));
			    DecoratedSeq.Append("[");
			    DecoratedSeq.Append(ThisPTMinLL.Name);
			    DecoratedSeq.Append("]");
			}
			CurrentSeqPos = ThisPTMinLL.Position;
			ThisPTMinLL = ThisPTMinLL.Next;
		    }
		    DecoratedSeq.Append(OutSequence.Substring(CurrentSeqPos));
		    string OutProForma = Convert.ToString(DecoratedSeq);
		    // I feel guilty for dumping garbage collection on the interpreter.
		    ThesePTMs.Next=null;
		    //If a named PTM has ambiguous localization, mark specific AAs that might hit it: "(KRGLKIVSLKMVKMSRDIAEK)[Methyl]" becomes "K[Methyl #g1]RGLK[#g1]IVSLK[#g1]MVK[#g1]MSRDIAEK[#g1]"
		    //NOT IMPLEMENTED YET
		    
		    if (SuppressUnknowns && ContainsUnknown) {
		    // Output Nothing
		    }
		    else {
			Console.Write(OutFile);
			Console.Write("\t");
			Console.Write(OutScan);
			Console.Write("\t");
			Console.Write(OutCharge);
			Console.Write("\t");
			Console.Write(OutAccession);
			Console.Write("\t");
			Console.Write(OutSequence);
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
