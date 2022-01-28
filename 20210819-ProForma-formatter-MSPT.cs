using System;
using System.IO;
using System.Data;
using System.Text;

namespace ProFormaFormatter {
    class Program {
	static void Main(string[] args)
	{
	    if (args.Length==0) {
		Console.WriteLine("Supply the name of the _IcTarget.tsv file for processing.");
		Environment.Exit(1);
	    }
	    DataTable datatable = new DataTable();
	    string FileString = args[0];
	    StreamReader streamreader = new StreamReader(FileString);
	    //skip the header
	    //	for (int i=0; i<27; i++) {
	    //	string Header=streamreader.ReadLine();
	    //  Console.WriteLine(Header);
	    //  }
	    char[] delimiter = new char[] { '\t' };
	    char[] PathDelimiters = new char[] { '\\','/' };
	    string FNameTrailer = "_IcTarget.tsv";
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

	    foreach (DataRow row in datatable.Rows) {
		//		string FName = Convert.ToString(row["Data file name"]);
		string OutScan = Convert.ToString(row["Scan"]);
		string OutProForma = Convert.ToString(row["Sequence"]);
		string OutSequence = OutProForma;
		string OutCharge = Convert.ToString(row["Charge"]);
		string OutAccession = Convert.ToString(row["ProteinName"]);
		int    OutMassAdded = 0;
		string PTMs = Convert.ToString(row["Modifications"]);
		float Probability = Convert.ToSingle(row["Probability"]);
		float Evalue = Convert.ToSingle(row["EValue"]);
		double OutEVal = -Math.Log(Convert.ToDouble(row["EValue"]));
		if ((Probability > 0.5) && (Evalue < 0.01)) {
		    // string[] FNameChunks = FName.Split(PathDelimiters);
		    string OutFile = FileString;
		    if (OutFile.EndsWith(FNameTrailer)) {
			OutFile = OutFile.Substring(0,OutFile.Length-FNameTrailer.Length);
		    }
		    // Parse out just the second bit of the UniProt accession
		    string[] AccessionArray = OutAccession.Split('|');
		    OutAccession = AccessionArray[1];
		    // Create a decorated sequence by incorporating PTM string data.
		    string[] PTMArray = PTMs.Split(',');
		    int CurrentOutProFormaPos = 0;
		    StringBuilder DecoratedOutProForma = new StringBuilder();
		    foreach (string ThisPTM in PTMArray) {
			if (ThisPTM.Length > 0) {
			    string[] PTMFields = ThisPTM.Split(' ');
			    int Pos = Convert.ToInt32(PTMFields[1]);
			    string PTMID = PTMFields[0];
			    if (PTMID == "Oxidation")
				OutMassAdded += 16;
			    else if (PTMID == "Methyl")
				OutMassAdded += 14;
			    else if (PTMID == "Acetyl")
				OutMassAdded += 42;
			    else if (PTMID == "Carbamidomethyl")
				OutMassAdded += 57;
			    else if (PTMID == "Phospho")
				OutMassAdded += 80;
			    if (Pos == 0) {
				DecoratedOutProForma.Append("[");
				DecoratedOutProForma.Append(Convert.ToString(PTMFields[0]));
				DecoratedOutProForma.Append("]-");
				DecoratedOutProForma.Append(OutProForma.Substring(CurrentOutProFormaPos, Pos-CurrentOutProFormaPos));
			    }
			    else {
				DecoratedOutProForma.Append(OutProForma.Substring(CurrentOutProFormaPos, Pos-CurrentOutProFormaPos));
				DecoratedOutProForma.Append("[");
				DecoratedOutProForma.Append(Convert.ToString(PTMFields[0]));
				DecoratedOutProForma.Append("]");
			    }
			    CurrentOutProFormaPos = Pos;
			}
		    }
		    DecoratedOutProForma.Append(OutProForma.Substring(CurrentOutProFormaPos));
		    OutProForma = Convert.ToString(DecoratedOutProForma);

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
