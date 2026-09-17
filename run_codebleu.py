from pathlib import Path
from codebleu import calc_codebleu

# Diretório base (onde está o script)
base_dir = Path(__file__).parent

# Caminhos dos arquivos
ref_path = base_dir / "NormalizedReferenceCode" / "ArrayDifference" / "ReferenceCodeArrayDifference.txt"
hyp_path = base_dir / "NormalizedGeneratedCode" / "GEMINI" / "ArrayDifference" / "Participant_4" / "GeneratedCodeGeminiArrayDifferenceParticipant4.txt"

# Leitura
refs = [ref_path.read_text(encoding="utf-8")]
hyps = [hyp_path.read_text(encoding="utf-8")]

# Cálculo
result = calc_codebleu(refs, hyps, lang="c_sharp")

print("CodeBLEU:", result["codebleu"])
print("N-gram:", result["ngram_match_score"])
print("Weighted N-gram:", result["weighted_ngram_match_score"])
print("AST Match:", result["syntax_match_score"])
print("Dataflow Match:", result["dataflow_match_score"])