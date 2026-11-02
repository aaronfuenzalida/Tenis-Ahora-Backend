"""
Prueba de humo de la API de prueba Tenis Ahora.

Uso:  python tests/smoke.py [http://localhost:5090/api]

Recorre autenticación, CRUD genérico y los endpoints de negocio principales contra una API
corriendo, y falla (exit 1) si alguna respuesta no es la esperada. Solo usa la biblioteca
estándar de Python.
"""
import base64
import json
import sys
from datetime import date, datetime, timedelta
import urllib.error
import urllib.request

BASE = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5090/api"
TOKEN = None
RESULTS = []


def call(method, path, body=None, auth=True, expect=None):
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(BASE + path, data=data, method=method)
    req.add_header("Content-Type", "application/json")
    if auth and TOKEN:
        req.add_header("Authorization", "Bearer " + TOKEN)
    try:
        with urllib.request.urlopen(req, timeout=10) as r:
            status, payload = r.status, r.read().decode()
    except urllib.error.HTTPError as e:
        status, payload = e.code, e.read().decode()
    try:
        parsed = json.loads(payload) if payload else None
    except json.JSONDecodeError:
        parsed = payload
    if expect is not None and status != expect:
        raise AssertionError(f"{method} {path} -> {status} (esperado {expect}): {payload[:300]}")
    return status, parsed


def download(path):
    req = urllib.request.Request(BASE + path)
    if TOKEN:
        req.add_header("Authorization", "Bearer " + TOKEN)
    try:
        with urllib.request.urlopen(req, timeout=10) as r:
            return r.status, r.headers.get("Content-Type"), r.read()
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get("Content-Type"), e.read()


def in_days(n):
    return (date.today() + timedelta(days=n)).isoformat()


def check(name, cond, detail=""):
    RESULTS.append((name, bool(cond), detail))
    mark = "OK " if cond else "FAIL"
    print(f"[{mark}] {name}{(' - ' + detail) if detail else ''}")


# Tarjetas de prueba de Mercado Pago (ficticias). El nombre del titular define el resultado.
VISA_CREDITO = "4509 9535 6623 3704"
VISA_DEBITO = "4002 7686 9439 5619"


def pay(amount, method="Mercado Pago (QR)", number=None, holder="APRO", cvv="123", expiry="11/30", expect=201):
    body = {"paymentMethod": method, "amount": amount, "concept": "smoke"}
    if number:
        body["card"] = {"number": number, "holder": holder, "cvv": cvv, "expiry": expiry}
    st, tx = call("POST", "/pagos", body)
    if expect is not None and st != expect:
        raise AssertionError(f"POST /pagos -> {st} (esperado {expect}): {tx}")
    return st, tx


def stock_of(category):
    st, items = call("GET", "/stock", expect=200)
    return next(i for i in items if i["category"] == category)


def login_as(email, full=False):
    st, auth = call("POST", "/auth/login", {"email": email, "password": "123456"}, auth=False, expect=200)
    return (auth["token"], auth) if full else auth["token"]


def main():
    global TOKEN
    st, h = call("GET", "/health", auth=False, expect=200)
    check("health", h.get("status") == "ok", f"v{h.get('version')} sqlite {h.get('sqlite')}")

    st, _ = call("GET", "/usuarios", auth=False)
    check("rutas protegidas sin token -> 401", st == 401)

    st, _ = call("POST", "/auth/login", {"email": "admin@tenisahora.com", "password": "incorrecta"}, auth=False)
    check("login con contraseña incorrecta -> 401", st == 401)

    st, auth = call("POST", "/auth/login", {"email": "admin@tenisahora.com", "password": "123456"}, auth=False, expect=200)
    TOKEN = auth["token"]
    check("login admin", auth.get("rol") == "Empleado" and TOKEN, auth.get("nombre"))

    st, me = call("GET", "/auth/yo", expect=200)
    check("token válido en /auth/yo", me.get("email") == "admin@tenisahora.com")

    # Registro
    email = f"smoke{__import__('time').time_ns()}@test.com"
    st, reg = call("POST", "/auth/registrar", {"nombre": "Smoke", "apellido": "Test", "email": email, "password": "1234", "direccion": "x", "numeroTelefono": "1"}, auth=False, expect=201)
    check("registro de socio", reg.get("rol") == "Socio")
    st, _ = call("POST", "/auth/registrar", {"nombre": "Smoke", "apellido": "Test", "email": email, "password": "1234"}, auth=False)
    check("registro duplicado -> 409", st == 409)
    call("DELETE", f"/usuarios/{reg['id']}", expect=200)

    # Reset para partir de datos conocidos
    st, r = call("POST", "/admin/reset", expect=200)
    check("reset + seed", r.get("documents", 0) > 50, f"{r.get('documents')} documentos")

    # CRUD genérico: tipos de cancha
    st, ct = call("POST", "/tipos-cancha", {"name": "Sintético", "label": "Césped Sintético", "basePricePerHour": 5000, "maxStudentsPerClass": 40}, expect=201)
    check("alta tipo de cancha (cupo recortado a 30)", ct["maxStudentsPerClass"] == 30 and ct["id"].startswith("ct-"))
    st, _ = call("POST", "/tipos-cancha", {"name": "sintético"})
    check("tipo de cancha duplicado -> 409", st == 409)
    st, ct2 = call("PUT", f"/tipos-cancha/{ct['id']}", {"basePricePerHour": 5500}, expect=200)
    check("modificar tipo de cancha", ct2["basePricePerHour"] == 5500 and ct2["label"] == "Césped Sintético")
    st, _ = call("DELETE", "/tipos-cancha/ct-ladrillo")
    check("borrar tipo en uso -> 409", st == 409)
    st, _ = call("DELETE", f"/tipos-cancha/{ct['id']}", expect=200)
    st, _ = call("GET", f"/tipos-cancha/{ct['id']}")
    check("tipo eliminado -> 404", st == 404)

    # Canchas: estado y disponibilidad
    st, c = call("PATCH", "/canchas/cancha-1/estado", {"status": "mantenimiento", "maintenanceNotes": "riego"}, expect=200)
    check("cancha a mantenimiento", c["status"] == "mantenimiento")
    singles = [{"name": "Juan Sánchez", "dni": "35.678.912"}, {"name": "Melina Romo", "dni": "39.104.556"}]
    st, _ = call("POST", "/reservas", {"courtId": "cancha-1", "date": in_days(5), "startTime": "10:00", "durationHours": 1, "playersType": "singles", "participants": singles})
    check("reserva en cancha bloqueada -> 409 (RD06)", st == 409)
    call("PATCH", "/canchas/cancha-1/estado", {"status": "disponible"}, expect=200)
    st, c = call("PUT", "/canchas/cancha-1/disponibilidad", {"days": ["Lunes", "Martes"], "openTime": "09:00", "closeTime": "21:00"}, expect=200)
    check("disponibilidad de cancha", c["availability"]["days"] == ["Lunes", "Martes"] and c["availability"]["slotMinutes"] == 60)
    st, _ = call("PUT", "/canchas/cancha-1/disponibilidad", {"days": []})
    check("disponibilidad sin días -> 400", st == 400)

    # Pasarela simulada: solo tarjetas ficticias; el titular define el resultado
    st, tx = pay(1000, "Tarjeta de Crédito", VISA_CREDITO)
    check("pago con tarjeta de prueba aprobado", tx["status"] == "approved" and tx["id"].startswith("MP-") and tx["cardLast4"] == "3704" and "number" not in tx)
    st, _ = pay(1000, "Tarjeta de Crédito", "4111 1111 1111 1111", expect=None)
    check("tarjeta que no es de prueba -> 400", st == 400)
    st, _ = pay(1000, "Tarjeta de Crédito", VISA_DEBITO, expect=None)
    check("tarjeta de débito como crédito -> 400", st == 400)
    st, rej = pay(1000, "Tarjeta de Débito", VISA_DEBITO, holder="FUND", expect=None)
    check("titular FUND -> rechazado por fondos (402)", st == 402 and "saldo" in rej["error"])
    st, _ = pay(1000, "Tarjeta de Débito", VISA_DEBITO, cvv="999", expect=None)
    check("código de seguridad inválido -> 402", st == 402)
    st, _ = pay(1000, "Tarjeta de Débito", VISA_DEBITO, expiry="01/20", expect=None)
    check("tarjeta vencida -> 402", st == 402)
    st, _ = pay(0, expect=None)
    check("pago sin importe -> 400", st == 400)

    # Reservas (TC_003): alta pendiente de pago, seña, validaciones
    players4 = [{"name": "Juan Sánchez", "dni": "35.678.912", "isLead": True}, {"name": "Franco Medina", "dni": "37.998.045"},
                {"name": "Ignacio Torres", "dni": "34.402.566"}, {"name": "Lucas Pereyra", "dni": "36.887.213"}]
    booking = {"courtId": "cancha-2", "date": in_days(10), "startTime": "10:00", "durationHours": 2,
               "playersType": "dobles", "participants": players4, "totalCost": 1, "depositPaid": 1}
    st, res = call("POST", "/reservas", booking, expect=201)
    rid = res["data"]["id"]
    check("alta de reserva queda pendiente de pago con costo de la cancha (RD01)",
          res["data"]["status"] == "pendiente_pago" and res["data"]["totalCost"] == 9600 and res["data"]["depositAmount"] == 4800
          and res["data"]["endTime"] == "12:00" and "receipt" not in res, rid)
    st, _ = call("POST", "/reservas", dict(booking, durationHours=3))
    check("reserva de más de 2 horas -> 400", st == 400)
    st, _ = call("POST", "/reservas", dict(booking, date=in_days(-1)))
    check("reserva con fecha pasada -> 400", st == 400)
    st, _ = call("POST", "/reservas", dict(booking, participants=players4[:3]))
    check("dobles con 3 jugadores -> 400", st == 400)
    st, _ = call("POST", "/reservas", dict(booking, playersType="singles", participants=[players4[0], players4[0]]))
    check("jugador repetido -> 400", st == 400)
    st, _ = call("POST", "/reservas", dict(booking, startTime="11:00", participants=[{"name": "A", "dni": "1"}, {"name": "B", "dni": "2"}], playersType="singles"))
    check("cancha ya reservada en esa franja -> 409", st == 409)
    st, _ = call("POST", "/reservas", dict(booking, courtId="cancha-3"))
    check("jugador con otra reserva a la misma hora -> 409", st == 409)
    st, _ = call("POST", "/reservas", dict(booking, date=in_days(31)))
    check("reserva a más de 30 días -> 400 (RD05)", st == 400)
    st, _ = call("POST", f"/reservas/{rid}/iniciar")
    check("iniciar una reserva sin seña -> 409 (RD01)", st == 409)
    st, _ = call("POST", f"/reservas/{rid}/pagar-sena", {})
    check("seña sin pago -> 400", st == 400)
    st, small = pay(100)
    st, _ = call("POST", f"/reservas/{rid}/pagar-sena", {"transactionId": small["id"]})
    check("seña con un pago por otro importe -> 400", st == 400)
    st, qr = pay(4800)
    st, conf = call("POST", f"/reservas/{rid}/pagar-sena", {"transactionId": qr["id"]}, expect=200)
    check("seña con QR de Mercado Pago confirma la reserva y emite recibo (TC_005)",
          conf["data"]["status"] == "confirmada" and conf["data"]["depositPaid"] == 4800
          and conf["receipt"]["transactionId"] == qr["id"] and conf["receipt"]["totalPaid"] == 4800, conf["receipt"]["id"])
    st, rec = call("GET", f"/recibos/{conf['receipt']['id']}", expect=200)
    check("recibo de seña persistido con datos de la reserva", rec["reservationId"] == rid and in_days(10) in rec["items"][0]["description"])
    st, _ = call("POST", f"/reservas/{rid}/pagar-sena", {"transactionId": qr["id"]})
    check("pagar la seña dos veces -> 409", st == 409)
    st, upd = call("PUT", f"/reservas/{rid}", {"startTime": "11:00", "status": "finalizada"}, expect=200)
    check("modificar reserva recalcula el fin y no cambia el estado", upd["endTime"] == "13:00" and upd["status"] == "confirmada")
    st, _ = call("PUT", f"/reservas/{rid}", {"durationHours": 3})
    check("modificar a más de 2 horas -> 400", st == 400)
    st, _ = call("PUT", f"/reservas/{rid}", {"date": in_days(31)})
    check("mover reserva a más de 30 días -> 400 (RD05)", st == 400)

    # Stock (TC_001 / TC_002): sin equipamiento no se reserva; se descuenta al iniciar y vuelve al finalizar
    balls = stock_of("Pelotas")
    call("PATCH", f"/stock/{balls['id']}/ajuste", {"totalStock": 0}, expect=200)
    st, err = call("POST", "/reservas", dict(booking, courtId="cancha-3", startTime="15:00"))
    check("reserva sin pelotas en stock -> 409 (TC_001)", st == 409 and "pelotas" in err["error"])
    call("PATCH", f"/stock/{balls['id']}/ajuste", {"totalStock": balls["totalStock"]}, expect=200)
    before = {c: stock_of(c)["availableStock"] for c in ("Redes", "Pelotas", "Raquetas")}
    st, playing = call("POST", f"/reservas/{rid}/iniciar", expect=200)
    during = {c: stock_of(c)["availableStock"] for c in ("Redes", "Pelotas", "Raquetas")}
    check("iniciar alquiler: en juego y descuenta red, pelotas y 4 raquetas (TC_002)",
          playing["status"] == "en_juego" and during == {"Redes": before["Redes"] - 1, "Pelotas": before["Pelotas"] - 1, "Raquetas": before["Raquetas"] - 4}, str(during))
    st, _ = call("PATCH", f"/stock/{stock_of('Raquetas')['id']}/ajuste", {"totalStock": 1})
    check("stock total menor a lo prestado -> 400", st == 400)
    st, _ = call("POST", f"/reservas/{rid}/cancelar")
    check("cancelar una reserva en juego -> 409", st == 409)
    st, _ = call("DELETE", f"/reservas/{rid}")
    check("eliminar una reserva en juego -> 409", st == 409)
    st, card = pay(4800, "Tarjeta de Débito", VISA_DEBITO)
    st, fin = call("POST", f"/reservas/{rid}/pagar-saldo", {"transactionId": card["id"]}, expect=200)
    after = {c: stock_of(c)["availableStock"] for c in ("Redes", "Pelotas", "Raquetas")}
    check("pagar saldo con débito finaliza y devuelve el equipamiento (TC_002)",
          fin["data"]["status"] == "finalizada" and fin["receipt"]["cardLast4"] == "5619" and after == before, str(after))

    # Cancelaciones (RD02-RD04)
    st, res2 = call("POST", "/reservas", dict(booking, startTime="16:00"), expect=201)
    rid2 = res2["data"]["id"]
    st, tx2 = pay(4800)
    call("POST", f"/reservas/{rid2}/pagar-sena", {"transactionId": tx2["id"]}, expect=200)
    st, canc = call("POST", f"/reservas/{rid2}/cancelar", expect=200)
    check("cancelar >= 6hs -> reembolso total", canc["isWithinFreeWindow"] and canc["refund"]["refundType"] == "total" and canc["refund"]["refundAmount"] == 4800)
    st, _ = call("POST", f"/reservas/{rid2}/cancelar")
    check("cancelar dos veces -> 409", st == 409)
    st, _ = call("POST", f"/reservas/{rid2}/pagar-sena", {"transactionId": tx2["id"]})
    check("un pago no se usa dos veces / reserva cancelada -> 409", st == 409)
    st, res3 = call("POST", "/reservas", dict(booking, startTime="18:00"), expect=201)
    st, canc3 = call("POST", f"/reservas/{res3['data']['id']}/cancelar", expect=200)
    check("cancelar una reserva pendiente de pago no genera reembolso ni cargo", canc3["data"]["status"] == "cancelada" and canc3["refund"] is None)
    soon = datetime.now().replace(minute=0, second=0, microsecond=0) + timedelta(hours=2)
    if soon.date() == date.today() and 8 <= soon.hour and soon.hour + 1 <= 22:
        st, res4 = call("POST", "/reservas", dict(booking, date=soon.date().isoformat(), startTime=soon.strftime("%H:%M"), durationHours=1, courtId="cancha-5"), expect=201)
        st, tx4 = pay(res4["data"]["depositAmount"])
        call("POST", f"/reservas/{res4['data']['id']}/pagar-sena", {"transactionId": tx4["id"]}, expect=200)
        st, canc4 = call("POST", f"/reservas/{res4['data']['id']}/cancelar", {"hoursInAdvance": 100}, expect=200)
        check("cancelar < 6hs (ignora hoursInAdvance del cliente) -> cargo 50% + reembolso parcial",
              (not canc4["isWithinFreeWindow"]) and canc4["penaltyReceipt"]["totalPaid"] == res4["data"]["depositAmount"] / 2)
    else:
        print("[SKIP] cancelación fuera de plazo: no hay un turno dentro de las próximas 6 hs en el horario de la cancha")

    # Recibos: todo recibo lleva un pago aprobado
    st, _ = call("POST", "/recibos", {"clientName": "C", "clientDni": "1", "concept": "Cuota", "totalPaid": 500, "items": []})
    check("recibo sin pago -> 400", st == 400)
    st, tx5 = pay(500, "Tarjeta de Crédito", VISA_CREDITO)
    st, rec5 = call("POST", "/recibos", {"clientName": "C", "clientDni": "1", "concept": "Cuota", "totalPaid": 500, "items": [], "paymentMethod": "Efectivo", "transactionId": tx5["id"]}, expect=201)
    check("recibo con pago toma el medio de la transacción", rec5["paymentMethod"] == "Tarjeta de Crédito" and rec5["transactionId"] == tx5["id"])

    # Configuración del club (RD02-RD05)
    st, cfg = call("GET", "/configuracion", expect=200)
    check("configuración por defecto", cfg["freeCancellationHours"] == 6 and cfg["lateCancellationRetentionPercent"] == 50 and cfg["maxAdvanceBookingDays"] == 30)
    st, _ = call("PUT", "/configuracion", {"maxAdvanceBookingDays": 365})
    check("la configuración no se modifica por API -> 405", st == 405)

    # Torneos: inscripción, enfrentamiento, reglamento
    st, enr = call("POST", "/torneos/trn-02/inscripciones", {"name": "Jugadora Test", "dni": "1", "partnerName": "Pareja"}, expect=201)
    check("inscripción a torneo", enr["participant"]["name"] == "Jugadora Test")
    st, _ = call("POST", "/torneos/trn-02/inscripciones", {"name": "Otra", "dni": "2", "partnerName": "Jugadora Test", "partnerDni": "1"})
    check("inscripción repetida al torneo -> 409", st == 409)
    st, m = call("POST", "/torneos/trn-02/enfrentamientos", {"round": "Cuartos", "p1": "A", "p2": "B", "court": "Cancha 3", "date": "2026-11-14 10:00"}, expect=201)
    st, m2 = call("PUT", f"/torneos/trn-02/enfrentamientos/{m['match']['id']}", {"score": "6-1, 6-2", "winner": "A", "status": "finalizado"}, expect=200)
    check("enfrentamiento de torneo alta + resultado", m2["match"]["winner"] == "A")
    st, _ = call("POST", "/torneos/trn-02/enfrentamientos", {"p1": "A", "p2": "A"})
    check("enfrentamiento con el mismo jugador -> 400", st == 400)
    st, t = call("PUT", "/torneos/trn-02/reglamento", {"internalRules": "Reglas X"}, expect=200)
    check("reglamento de torneo", t["internalRules"] == "Reglas X")

    # Documentos: subida, descarga y reglamento adjunto (RF041 / RF048)
    pdf = b"%PDF-1.4\n% smoke\n%%EOF\n"
    st, doc = call("POST", "/documentos", {"name": "Reglamento Smoke.pdf", "data": base64.b64encode(pdf).decode()}, expect=201)
    check("subir PDF", doc["mimeType"] == "application/pdf" and doc["size"] == len(pdf) and "data" not in doc)
    st, _ = call("POST", "/documentos", {"name": "nota.txt", "data": base64.b64encode(b"hola").decode()})
    check("subir un .txt -> 400", st == 400)
    st, _ = call("POST", "/documentos", {"name": "grande.pdf", "data": base64.b64encode(b"%PDF-" + b"0" * (5 << 20)).decode()})
    check("subir más de 5 MB -> 400", st == 400)
    st, ctype, raw = download(f"/documentos/{doc['id']}")
    check("descargar PDF", st == 200 and ctype == "application/pdf" and raw == pdf)
    st, t = call("PUT", "/torneos/trn-02/reglamento", {"internalRulesDocument": {"id": doc["id"]}}, expect=200)
    check("adjuntar reglamento sin pisar el texto", t["internalRulesDocument"]["name"] == "Reglamento Smoke.pdf" and t["internalRules"] == "Reglas X")
    st, _ = call("PUT", "/ligas/liga-01/reglamento", {"internalRulesDocument": {"id": "doc-inexistente"}})
    check("reglamento con documento inexistente -> 400", st == 400)
    st, t = call("PUT", "/torneos/trn-02/reglamento", {"internalRulesDocument": None}, expect=200)
    check("quitar documento del reglamento", "internalRulesDocument" not in t)
    call("DELETE", f"/torneos/trn-02/inscripciones/{enr['participant']['id']}", expect=200)

    # Ligas: resultado actualiza tabla
    st, lg = call("GET", "/ligas/liga-01", expect=200)
    p1, p2 = lg["participants"][0]["name"], lg["participants"][1]["name"]
    st, lm = call("POST", "/ligas/liga-01/enfrentamientos", {"round": "Jornada Test", "p1": p1, "p2": p2}, expect=201)
    pts_before = next(s["points"] for s in lg["standings"] if s["name"] == p1)
    st, scored = call("POST", f"/ligas/liga-01/enfrentamientos/{lm['match']['id']}/resultado", {"score": "6-4, 6-4", "winner": p1}, expect=200)
    pts_after = next(s["points"] for s in scored["standings"] if s["name"] == p1)
    check("resultado de liga suma 3 puntos", pts_after == pts_before + 3, f"{p1}: {pts_before} -> {pts_after}")
    st, scored2 = call("POST", f"/ligas/liga-01/enfrentamientos/{lm['match']['id']}/resultado", {"score": "6-4, 6-4", "winner": p1}, expect=200)
    pts_again = next(s["points"] for s in scored2["standings"] if s["name"] == p1)
    check("recargar resultado no duplica puntos", pts_again == pts_after)

    # Clases: cupo y asistencia
    st, cls = call("POST", "/clases", {"name": "Clase Smoke", "kind": "entrenamiento", "coachId": "coach-02", "coachName": "X", "maxCapacity": 2, "sessions": ["01/11", "03/11"]}, expect=201)
    cid = cls["id"]
    st, s1 = call("POST", f"/clases/{cid}/alumnos", {"name": "Alumno 1", "dni": "1"}, expect=201)
    st, _ = call("POST", f"/clases/{cid}/alumnos", {"name": "Alumno 1", "dni": "1"})
    check("alumno repetido en la clase -> 409", st == 409)
    call("POST", f"/clases/{cid}/alumnos", {"name": "Alumno 2", "dni": "2"}, expect=201)
    st, _ = call("POST", f"/clases/{cid}/alumnos", {"name": "Alumno 3", "dni": "3"})
    check("cupo de clase respetado (RD10) -> 409", st == 409)
    st, _ = call("POST", "/clases", {"name": "Grande", "maxCapacity": 31})
    check("clase con cupo > 30 -> 400 (RD07)", st == 400)
    st, att = call("POST", f"/clases/{cid}/asistencia/alternar", {"studentId": s1["student"]["id"], "sessionIndex": 1}, expect=200)
    cls_now = next(c for c in att["classes"] if c["id"] == cid)
    st_now = next(s for s in cls_now["students"] if s["id"] == s1["student"]["id"])
    check("alumno nuevo arranca sin asistencia registrada", set(s1["student"]["attendance"]) == {"-"})
    check("alternar asistencia sin registrar -> P", st_now["attendance"][1] == "P")
    st, att = call("POST", f"/clases/{cid}/asistencia/alternar", {"studentId": s1["student"]["id"], "sessionIndex": 1}, expect=200)
    cls_now = next(c for c in att["classes"] if c["id"] == cid)
    st_now = next(s for s in cls_now["students"] if s["id"] == s1["student"]["id"])
    check("alternar asistencia P -> A", st_now["attendance"][1] == "A")
    st, att = call("POST", f"/clases/{cid}/asistencia/sesion", {"sessionIndex": 0, "status": "A"}, expect=200)
    cls_now = next(c for c in att["classes"] if c["id"] == cid)
    check("marcar sesión completa", all(s["attendance"][0] == "A" for s in cls_now["students"]))
    st, _ = call("POST", f"/clases/{cid}/asistencia/sesion", {"sessionIndex": 0, "status": "J"})
    check("estado de asistencia distinto de P/A -> 400", st == 400)
    call("DELETE", f"/clases/{cid}", expect=200)

    # Título habilitante obligatorio (RD08 / RD09)
    lic = {"titleName": "Profesor Smoke", "institution": "AAT", "licenseNumber": "AAT-1", "expiryDate": in_days(365)}
    st, _ = call("POST", "/profesores", {"name": "Prof. Sin Título", "type": "Profesor"})
    check("profesor sin título -> 400 (RD08)", st == 400)
    st, _ = call("POST", "/profesores", {"name": "Prof. Sin Documento", "type": "Profesor", "licenseDetails": lic})
    check("profesor sin documento del título -> 400 (RD08)", st == 400)
    st, prof = call("POST", "/profesores", {"name": "Prof. Smoke", "type": "Profesor", "licenseDetails": dict(lic, document={"id": doc["id"]})}, expect=201)
    check("profesor con título y documento", prof["licenseDetails"]["document"]["id"] == doc["id"] and prof["licenseDetails"]["status"] == "habilitado")
    st, _ = call("PUT", f"/profesores/{prof['id']}/certificacion", {"expiryDate": in_days(-1)}, expect=200)
    st, _ = call("POST", "/clases", {"name": "Clase Vencida", "kind": "clase", "coachId": prof["id"], "maxCapacity": 5})
    check("clase con título vencido -> 400 (RD09)", st == 400)
    st, _ = call("POST", "/clases-particulares", {"coachId": prof["id"], "kind": "clase", "studentName": "X", "studentDni": "1", "date": in_days(3), "time": "10:00 - 11:00"})
    check("turno particular con título vencido -> 400 (RD09)", st == 400)
    st, _ = call("PUT", f"/profesores/{prof['id']}", {"availableHours": "Lunes 10:00 - 12:00"}, expect=200)
    check("editar datos sin tocar el título no lo revalida", st == 200)
    call("DELETE", f"/profesores/{prof['id']}", expect=200)

    # Caja: factura, reembolso, descuentos, usuario
    st, inv = call("POST", "/facturas", {"clientName": "Cliente", "clientDni": "1", "concept": "Alquiler", "items": [{"description": "1h", "quantity": 1, "unitPrice": 5000}], "paymentMethod": "Tarjeta de Débito"}, expect=201)
    check("factura por el importe de los ítems", inv["total"] == 5000 and inv["id"].startswith("FC-0001-"))
    st, rf = call("POST", "/reembolsos", {"clientName": "C", "clientDni": "1", "reason": "x", "originalAmount": 1000, "refundAmount": 400}, expect=201)
    check("reembolso parcial", rf["refundType"] == "parcial")
    st, d = call("POST", "/descuentos", {"name": "Promo", "code": "promo9", "discountPercent": 9}, expect=201)
    check("alta descuento (código en mayúsculas)", d["code"] == "PROMO9")
    st, u = call("POST", "/usuarios", {"name": "Usuario Smoke", "dni": "40.000.000", "email": "usmoke@test.com", "phone": "1", "address": "x", "role": "client"}, expect=201)
    check("alta usuario sin exponer hash", "passwordHash" not in u and u["memberNumber"].startswith("TA-"))
    st, u2 = call("PUT", f"/usuarios/{u['id']}/descuentos", {"activeDiscounts": ["PROMO9", "PAQ10"]}, expect=200)
    check("asignar descuentos a usuario", u2["activeDiscounts"] == ["PROMO9", "PAQ10"])
    st, _ = call("PUT", f"/usuarios/{u['id']}/descuentos", {"activeDiscounts": ["NOEXISTE"]})
    check("descuento inexistente -> 400", st == 400)
    call("DELETE", f"/descuentos/{d['id']}", expect=200)
    st, u3 = call("GET", f"/usuarios/{u['id']}", expect=200)
    check("borrar descuento lo quita del usuario (cascada)", u3["activeDiscounts"] == ["PAQ10"])
    st, login = call("POST", "/auth/login", {"email": "usmoke@test.com", "password": "123456"}, auth=False, expect=200)
    check("usuario creado por admin inicia sesión con contraseña por defecto", login["rol"] == "Socio")
    admin_token, TOKEN = TOKEN, login["token"]
    st, _ = call("POST", "/admin/reset")
    check("reset con rol Socio -> 403", st == 403)
    TOKEN = admin_token
    call("DELETE", f"/usuarios/{u['id']}", expect=200)

    st, _ = call("GET", "/coleccion-inexistente")
    check("colección inexistente -> 404", st == 404)

    # Permisos por rol
    call("POST", "/admin/reset", expect=200)
    st, _ = call("POST", "/clases", {"name": "X", "kind": "clase", "coachId": "coach-02"})
    check("clase asignada a un entrenador -> 400", st == 400)
    st, _ = call("POST", "/usuarios", {"name": "P", "email": "p@test.com", "role": "coach"})
    check("usuario profesional sin ficha -> 400", st == 400)

    TOKEN = login_as("socio@tenisahora.com")
    socio_dni = "35.678.912"
    st, _ = call("GET", "/usuarios")
    check("socio no ve usuarios -> 403", st == 403)
    st, _ = call("PUT", "/usuarios/usr-01", {"role": "admin"})
    check("socio no se cambia el rol -> 403", st == 403)
    st, _ = call("POST", "/canchas", {"name": "X"})
    check("socio no crea canchas -> 403", st == 403)
    st, mine = call("GET", "/reservas", expect=200)
    check("socio ve solo sus reservas", mine and all(r["participants"][0]["dni"] == socio_dni for r in mine), f"{len(mine)} reservas")
    st, occ = call("GET", "/reservas/ocupacion", expect=200)
    check("ocupación muestra turnos ajenos sin datos personales", len(occ) > len(mine) and all("participants" not in r for r in occ))
    st, recs = call("GET", "/recibos", expect=200)
    check("socio ve solo sus recibos", recs and all(r["clientDni"] == socio_dni for r in recs))
    st, clases = call("GET", "/clases", expect=200)
    check("socio ve clases sin alumnos", clases and all("students" not in c for c in clases))
    own_booking = dict(booking, startTime="20:00", playersType="singles", participants=singles)
    st, _ = call("POST", "/reservas", dict(own_booking, participants=[{"name": "Otro", "dni": "1"}, singles[1]]))
    check("socio no reserva a nombre de otro -> 403", st == 403)
    st, own = call("POST", "/reservas", own_booking, expect=201)
    socio_token, TOKEN = TOKEN, admin_token
    st, admin_tx = pay(own["data"]["depositAmount"])
    TOKEN = socio_token
    st, _ = call("POST", f"/reservas/{own['data']['id']}/pagar-sena", {"transactionId": admin_tx["id"]})
    check("socio no usa un pago ajeno -> 403", st == 403)
    st, own_tx = pay(own["data"]["depositAmount"], "Tarjeta de Crédito", VISA_CREDITO)
    st, own_conf = call("POST", f"/reservas/{own['data']['id']}/pagar-sena", {"transactionId": own_tx["id"]})
    check("socio paga la seña de su reserva", st == 200 and own_conf["data"]["status"] == "confirmada")
    st, _ = call("POST", f"/reservas/{own['data']['id']}/iniciar")
    check("socio no inicia alquileres -> 403", st == 403)
    st, _ = call("POST", "/reservas/RES-2026-002/cancelar")
    check("socio no cancela reservas ajenas -> 403", st == 403)
    st, _ = call("POST", f"/reservas/{own['data']['id']}/cancelar")
    check("socio cancela su reserva", st == 200)
    st, cfg = call("GET", "/configuracion")
    check("socio consulta la configuración", st == 200 and cfg["privateLessonHourlyRate"] == 6000)
    st, _ = call("PUT", "/usuarios/usr-01", {"phone": "11-0000-0000"})
    check("socio edita su teléfono", st == 200)
    st, _ = call("POST", "/documentos", {"name": "x.pdf", "data": base64.b64encode(b"%PDF-1.4").decode()})
    check("socio no sube documentos -> 403", st == 403)
    st, ctype, raw = download("/documentos/doc-seed-coach-01")
    check("socio descarga documentos", st == 200 and raw.startswith(b"%PDF-"))
    st, _ = call("POST", "/torneos/trn-02/inscripciones", {"name": "Otro", "dni": "1"})
    check("socio no inscribe a otro -> 403", st == 403)
    st, _ = call("POST", "/torneos/trn-02/inscripciones", {"name": "Juan Sánchez", "dni": socio_dni})
    check("socio se inscribe a un torneo", st == 201)
    st, _ = call("POST", "/clases/cls-01/alumnos", {"name": "Juan Sánchez", "dni": socio_dni})
    check("socio se inscribe a una clase", st == 201)
    st, _ = call("POST", "/clases/cls-01/alumnos", {"name": "Juan Sánchez", "dni": socio_dni})
    check("socio no se inscribe dos veces a la misma clase -> 409", st == 409)
    st, clases = call("GET", "/clases", expect=200)
    check("socio ve en qué clases está inscripto", next(c for c in clases if c["id"] == "cls-01")["enrolled"] and not next(c for c in clases if c["id"] == "cls-02")["enrolled"])
    st, _ = call("POST", "/clases/cls-01/asistencia/sesion", {"sessionIndex": 0, "status": "P"})
    check("socio no toma asistencia -> 403", st == 403)
    lesson = {"coachId": "coach-01", "coachName": "Gustavo Ferrari", "studentName": "Juan Sánchez", "studentDni": socio_dni, "date": in_days(10), "time": "10:00 - 11:00"}
    st, _ = call("POST", "/clases-particulares", dict(lesson, kind="clase"))
    check("socio pide clase particular a un profesor", st == 201)
    st, _ = call("POST", "/clases-particulares", dict(lesson, kind="clase"))
    check("dos turnos particulares en el mismo horario -> 409", st == 409)
    st, _ = call("POST", "/clases-particulares", dict(lesson, kind="entrenamiento"))
    check("entrenamiento con un profesor -> 400", st == 400)

    TOKEN, prof = login_as("profesor@tenisahora.com", full=True)
    check("login profesor", prof["rol"] == "Profesor" and prof["coachId"] == "coach-01")
    st, clases = call("GET", "/clases", expect=200)
    check("profesor ve solo sus clases, con alumnos", clases and all(c["coachId"] == "coach-01" and "students" in c for c in clases))
    st, _ = call("GET", "/reservas")
    check("profesor no ve reservas -> 403", st == 403)
    st, socios = call("GET", "/socios")
    check("profesor consulta el padrón reducido para elegir alumno", st == 200 and socios and "phone" not in socios[0])
    st, _ = call("POST", "/clases/cls-01/asistencia/sesion", {"sessionIndex": 0, "status": "P"})
    check("profesor toma asistencia en su clase", st == 200)
    st, _ = call("POST", "/clases/cls-04/asistencia/sesion", {"sessionIndex": 0, "status": "P"})
    check("profesor no toma asistencia en clase ajena -> 403", st == 403)
    st, p = call("POST", "/clases-particulares", dict(lesson, coachId="coach-02", kind="entrenamiento", time="11:00 - 12:00"), expect=201)
    check("clase particular del profesor queda a su nombre y como clase", p["coachId"] == "coach-01" and p["kind"] == "clase")
    st, c1 = call("PUT", "/profesores/coach-01", {"availableHours": "Smoke", "type": "Entrenador"}, expect=200)
    check("profesor edita su ficha sin cambiar su tipo", c1["availableHours"] == "Smoke" and c1["type"] == "Profesor")
    st, _ = call("PUT", "/profesores/coach-02", {"availableHours": "Smoke"})
    check("profesor no edita fichas ajenas -> 403", st == 403)

    TOKEN, ent = login_as("entrenador@tenisahora.com", full=True)
    check("login entrenador", ent["rol"] == "Entrenador")
    st, p = call("POST", "/clases-particulares", dict(lesson, coachId="coach-02", time="12:00 - 13:00"), expect=201)
    check("clase particular del entrenador queda como entrenamiento", p["kind"] == "entrenamiento")
    TOKEN = admin_token

    # Dejar la base limpia
    call("POST", "/admin/reset", expect=200)

    failed = [r for r in RESULTS if not r[1]]
    print(f"\n{len(RESULTS) - len(failed)}/{len(RESULTS)} pruebas OK")
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    try:
        main()
    except AssertionError as e:
        print("[FAIL]", e)
        sys.exit(1)
    except urllib.error.URLError as e:
        print("No se pudo conectar con la API:", e)
        sys.exit(2)
