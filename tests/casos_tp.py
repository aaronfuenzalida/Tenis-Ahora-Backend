"""
Casos de prueba TC_001 a TC_005 del TP (sección 7.1, "Testing de la aplicación").

Uso:  python tests/casos_tp.py [http://localhost:5090/api]

Corre los cinco casos contra una API levantada y muestra el estado de cada uno (Pass / Fail), como
la columna Status del documento. Cada caso también se puede correr solo:

    python tests/tc_001_bloqueo_sin_stock.py
    python tests/tc_002_descuento_restitucion_stock.py
    python tests/tc_003_validacion_reserva.py
    python tests/tc_004_torneo_cupo_fixture.py
    python tests/tc_005_cobro_sena_recibo.py

Cada caso parte del seed (POST /admin/reset), así que borra los datos que se hayan cargado a mano,
igual que smoke.py. Las fechas del documento (octubre de 2026) se reemplazan por fechas relativas a
hoy, porque la API rechaza reservas en el pasado y a más de 30 días.
"""
import sys
import urllib.error

import smoke

ADMIN = "admin@tenisahora.com"


def ejecutar(casos):
    resumen = []
    try:
        smoke.TOKEN = smoke.login_as(ADMIN)
        for caso in casos:
            print(f"\n{caso.ID} - {caso.TITULO}")
            desde = len(smoke.RESULTS)
            try:
                caso.run()
                ok = all(r[1] for r in smoke.RESULTS[desde:])
            except AssertionError as e:
                print("[FAIL]", e)
                ok = False
            resumen.append((caso.ID, ok))
        smoke.call("POST", "/admin/reset", expect=200)
    except urllib.error.URLError as e:
        print("No se pudo conectar con la API:", e)
        return 2

    print()
    for tc, ok in resumen:
        print(f"{tc}  {'Pass' if ok else 'Fail'}")
    return 0 if all(ok for _, ok in resumen) else 1


if __name__ == "__main__":
    import tc_001_bloqueo_sin_stock
    import tc_002_descuento_restitucion_stock
    import tc_003_validacion_reserva
    import tc_004_torneo_cupo_fixture
    import tc_005_cobro_sena_recibo

    sys.exit(ejecutar([
        tc_001_bloqueo_sin_stock,
        tc_002_descuento_restitucion_stock,
        tc_003_validacion_reserva,
        tc_004_torneo_cupo_fixture,
        tc_005_cobro_sena_recibo,
    ]))
