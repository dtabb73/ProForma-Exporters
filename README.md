# ProForma-Exporters
Tools for creating ProForma v2 reports from ProSight PD, TopPIC, MSPathFinderT, and pTop top-down ID software.

Each of these .cs files represents a separate tool for converting the outputs of top-down MS/MS search engines into tab-separated value tables that include the following columns:
1) RAW filename
2) Scan number within RAW file
3) PrSM precursor charge
4) Stripped UniProt accession
5) Truncated sequence for PrSM without PTMs
6) Integer sum of rounded PTM masses
7) ProForma string representing sequence and PTMs
8) Negative log E-Value for PrSM

At present, these headers do not appear in the output of the tools so that one can append these tables across multiple output files.

The MSPathFinderT tool reads an individual "IcTarget.tsv" files, expecting that users will have a different output for each input raw file.  Correspondingly, one would likely run 20210819-ProForma-formatter-MSPT on the first output file with "> MSPT.tsv" to redirect output from the tool into a text file by that name.  On the remaining outputs, one would probably use ">> MSPT.tsv" to append the result to that started on the first file.  MSPathFinderT is also unique among the search engines that its output PrSMs will be unfiltered; the script itself imposes two thresholds, one to require expectation values below 0.01 for each PrSM and another to require probability scores above 0.5 for each PrSM.

The ProSight Proteome Discoverer tool expects to receive a "PrSMs.txt" file that was created in Proteome Discoverer 2.5 by ProSight PD 4.0.  Before exporting these text files from Proteome Discoverer, users should be sure to change three fields from the default exports: Disable the bulky "External Top-Down Displays" field, Enable the "Fragmentation Scan(s)" field, and Enable the "Original Precursor Charge" field.  If the search was performed without filtering on the basis of FDR, the user should add the "--medium" or "--high" option to the command line.

The pTop tool is designed to work with both pTop 1.2 and pTop 2.0 outputs, though the former creates outputs for individual raw files and the latter created a single report for all output files.  Correspondingly, users should supply the name of an individual search_task_filter.csv or pTop_filtered.csv file for processing.  PrSM filtering is assumed to have taken place within pTop.

The TopPIC tool has mainly been used against conjoint reports from TopPIC 1.4.13 (and an early version of 1.5) that were created with the "-c" flag in TopPIC.  The file extension is "ms2_toppic_prsm.tsv".  These reports contain considerably more information than do the other output files enumerated above, such as localization ambiguity of PTMs, hypotheses about mass shifts that have not been pre-specified by users, and cases where multiple protein accessions contain the truncated sequences of a given proteoform.  The TopPIC tool has the ability to screen out any PrSM that contains an unidentified mass shift; users simply append "--suppress" to the command line to prevent them from being included in the output.
