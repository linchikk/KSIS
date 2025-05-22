from flask import Flask, request, send_file, jsonify, abort, render_template, send_from_directory
import os
import shutil
import email.utils
from werkzeug.utils import secure_filename

app = Flask(__name__)
app.config['STORAGE_ROOT'] = os.path.join(os.getcwd(), 'storage')
app.config['MAX_CONTENT_LENGTH'] = 16 * 1024 * 1024  # 16 MB limit


def ensure_storage_root():
    os.makedirs(app.config['STORAGE_ROOT'], exist_ok=True)


def get_directory_listing(path):
    entries = []
    try:
        for name in sorted(os.listdir(path)):
            entry_path = os.path.join(path, name)
            is_dir = os.path.isdir(entry_path)
            size = os.path.getsize(entry_path) if not is_dir else 0
            mtime = os.path.getmtime(entry_path)
            entries.append({
                'name': name,
                'type': 'directory' if is_dir else 'file',
                'size': size,
                'mtime': email.utils.formatdate(mtime, usegmt=True)
            })
    except Exception as e:
        print(f"Directory listing error: {str(e)}")
    return entries


@app.route('/')
def index():
    return render_template('browser.html')


@app.route('/static/<path:filename>')
def static_files(filename):
    return send_from_directory('static', filename)


@app.route('/download/<path:filepath>')
def download_file(filepath):
    full_path = os.path.join(app.config['STORAGE_ROOT'], filepath)
    if os.path.isfile(full_path):
        return send_file(full_path, as_attachment=True)
    abort(404)


@app.route('/<path:filepath>', methods=['GET', 'PUT', 'DELETE', 'HEAD'])
def handle_file(filepath):
    full_path = os.path.abspath(os.path.join(app.config['STORAGE_ROOT'], filepath))

    # Security check
    if not full_path.startswith(app.config['STORAGE_ROOT']):
        abort(400, "Invalid path")

    # Resource existence check
    if not os.path.exists(full_path):
        abort(404)

    # Handle files
    if os.path.isfile(full_path):
        if request.method == 'GET':
            return send_file(full_path)

        elif request.method == 'HEAD':
            headers = {
                'Content-Length': os.path.getsize(full_path),
                'Last-Modified': email.utils.formatdate(
                    os.path.getmtime(full_path), usegmt=True)
            }
            return ('', 200, headers)

        elif request.method == 'DELETE':
            os.remove(full_path)
            return '', 204

    # Handle directories
    elif os.path.isdir(full_path):
        if request.method == 'GET':
            entries = get_directory_listing(full_path)
            return jsonify({
                'path': filepath,
                'files': entries
            }), 200, {'Content-Type': 'application/json'}

        elif request.method == 'DELETE':
            shutil.rmtree(full_path)
            return '', 204

    # Handle PUT requests
    if request.method == 'PUT':
        if os.path.isdir(full_path):
            abort(400, "Cannot overwrite directory with file")

        file_existed = os.path.isfile(full_path)
        os.makedirs(os.path.dirname(full_path), exist_ok=True)

        try:
            with open(full_path, 'wb') as f:
                f.write(request.get_data())
        except Exception as e:
            abort(500, f"File write error: {str(e)}")

        return ('', 200) if file_existed else ('', 201)

    abort(405)


if __name__ == '__main__':
    ensure_storage_root()
    app.run(debug=True)