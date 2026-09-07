"""Reuse the banked validation recipe in this investigation's own output root."""
import pathlib,sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
recipe=ROOT/'docs/engineering-evidence/m13.2-terrain-shading/validate.py'
source=recipe.read_text().replace("OUT=ROOT/'build/m13.2-terrain-shading'","OUT=ROOT/'build/ksa-terrain-convergence'")
# The complete headless/GPU suites are recorded separately. This bounded finish
# verifies the normal deployed window, native GPU, production assets and routes.
source=source.replace("for category in ('gpu',):","for category in ():")
source=source.replace("for selection in ['M12D-P2S5G','Florida facility support','Single canonical physical','Generation-4 physical renderer','Earth route convergence','Production material noise value preservation','Production window lifecycle']:","for selection in ['Production window lifecycle']:")
source=source.replace("            execute('regional-window-'+config,[exe,'--test=Live NCSM1 regional physical residency'])","")
source=source.replace("'NCSM1 regional ready:',","'NCSM1 regional ready:','P2S5C3 CPU timing:','P2S5C3 frame timing:','P2S5C3 GPU timing:','NCSM1 regional GPU work:',")
exec(compile(source,str(recipe),'exec'),dict(__name__='__main__',__file__=str(recipe)))
